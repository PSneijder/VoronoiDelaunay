using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using VoronoiDelaunay.Logic.Delaunay.LR;
using VoronoiDelaunay.Logic.Geo;
using LineSegment = VoronoiDelaunay.Logic.Geo.LineSegment;

namespace VoronoiDelaunay.Logic.Delaunay
{
    public sealed class Voronoi
        : IDisposable
    {
        private static readonly Random R = new Random();
        private List<Edge> _edges;

        // TODO generalize this so it doesn't have to be a rectangle;
        // then we can make the fractal voronois-within-voronois
        private SiteList _sites;
        private Dictionary<Point, Site> _sitesIndexedByLocation;
        private List<Triangle> _triangles;

        private Site _fortunesAlgorithmBottomMostSite;

        public Voronoi(List<Point> points, List<Color> colors, Rect plotBounds)
        {
            _sites = new SiteList();
            _sitesIndexedByLocation = new Dictionary<Point, Site>(); // XXX: Used to be Dictionary(true) -- weak refs. 
            AddSites(points, colors);
            this.PlotBounds = plotBounds;
            _triangles = new List<Triangle>();
            _edges = new List<Edge>();
            FortunesAlgorithm();
        }

        public Rect PlotBounds { get; private set; }

        public void Dispose()
        {
            int i, n;
            if (_sites != null)
            {
                _sites.Dispose();
                _sites = null;
            }
            if (_triangles != null)
            {
                n = _triangles.Count;
                for (i = 0; i < n; ++i)
                {
                    _triangles[i].Dispose();
                }
                _triangles.Clear();
                _triangles = null;
            }
            if (_edges != null)
            {
                n = _edges.Count;
                for (i = 0; i < n; ++i)
                {
                    _edges[i].Dispose();
                }
                _edges.Clear();
                _edges = null;
            }
            //			_plotBounds = null;
            _sitesIndexedByLocation = null;
        }

        private void AddSites(List<Point> points, List<Color> colors)
        {
            var length = points.Count;
            for (var i = 0; i < length; ++i)
            {
                AddSite(points[i], colors != null ? colors[i] : Colors.Transparent, i);
            }
        }

        private void AddSite(Point p, Color color, int index)
        {
            if (_sitesIndexedByLocation.ContainsKey(p))
                return; // Prevent duplicate site! (Adapted from https://github.com/nodename/as3delaunay/issues/1)
            var weight = R.NextDouble()*100f;
            var site = Site.Create(p, (uint) index, weight, color);
            _sites.Add(site);
            _sitesIndexedByLocation[p] = site;
        }

        public List<Edge> Edges()
        {
            return _edges;
        }

        public List<Point> Region(Point p)
        {
            var site = _sitesIndexedByLocation[p];
            if (site == null)
            {
                return new List<Point>();
            }
            return site.Region(PlotBounds);
        }

        // TODO: bug: if you call this before you call region(), something goes wrong :(
        public List<Point> NeighborSitesForSite(Point coord)
        {
            var points = new List<Point>();
            var site = _sitesIndexedByLocation[coord];
            if (site == null)
            {
                return points;
            }
            var sites = site.NeighborSites();
            foreach (var neighbor in sites)
            {
                points.Add(neighbor.Coord);
            }
            return points;
        }

        public List<Circle> Circles()
        {
            return _sites.Circles();
        }

        public List<LineSegment> VoronoiBoundaryForSite(Point coord)
        {
            return DelaunayHelpers.VisibleLineSegments(DelaunayHelpers.SelectEdgesForSitePoint(coord, _edges));
        }

        public List<LineSegment> DelaunayLinesForSite(Point coord)
        {
            return DelaunayHelpers.DelaunayLinesForEdges(DelaunayHelpers.SelectEdgesForSitePoint(coord, _edges));
        }

        public List<LineSegment> VoronoiDiagram()
        {
            return DelaunayHelpers.VisibleLineSegments(_edges);
        }

        public List<LineSegment> DelaunayTriangulation( /*BitmapData keepOutMask = null*/)
        {
            return
                DelaunayHelpers.DelaunayLinesForEdges(DelaunayHelpers.SelectNonIntersectingEdges( /*keepOutMask,*/_edges));
        }

        public List<LineSegment> Hull()
        {
            return DelaunayHelpers.DelaunayLinesForEdges(HullEdges());
        }

        private List<Edge> HullEdges()
        {
            return _edges.FindAll(edge => edge.IsPartOfConvexHull());
        }

        public List<Point> HullPointsInOrder()
        {
            var hullEdges = HullEdges();

            var points = new List<Point>();
            if (hullEdges.Count == 0)
            {
                return points;
            }

            var reorderer = new EdgeReorderer(hullEdges, VertexOrSite.Site);
            hullEdges = reorderer.Edges;
            var orientations = reorderer.EdgeOrientations;
            reorderer.Dispose();

            var n = hullEdges.Count;
            for (var i = 0; i < n; ++i)
            {
                var edge = hullEdges[i];
                var orientation = orientations[i];
                points.Add(edge.Site(orientation).Coord);
            }
            return points;
        }

        public List<LineSegment> SpanningTree(KruskalType type = KruskalType.Minimum /*, BitmapData keepOutMask = null*/)
        {
            var edges = DelaunayHelpers.SelectNonIntersectingEdges( /*keepOutMask,*/_edges);
            var segments = DelaunayHelpers.DelaunayLinesForEdges(edges);
            return DelaunayHelpers.Kruskal(segments, type);
        }

        public List<List<Point>> Regions()
        {
            return _sites.Regions(PlotBounds);
        }

        public List<Color> SiteColors( /*BitmapData referenceImage = null*/)
        {
            return _sites.SiteColors( /*referenceImage*/);
        }

        /**
         * 
         * @param proximityMap a BitmapData whose regions are filled with the site index values; see PlanePointsCanvas::fillRegions()
         * @param x
         * @param y
         * @return coordinates of nearest Site to (x, y)
         * 
         */

        public Point? NearestSitePoint( /*BitmapData proximityMap,*/ double x, double y)
        {
            return _sites.NearestSitePoint( /*proximityMap,*/x, y);
        }

        public List<Point> SiteCoords()
        {
            return _sites.SiteCoords();
        }

        private void FortunesAlgorithm()
        {
            Vertex vertex;
            var newintstar = default(Point);
            Edge edge;

            var dataBounds = _sites.GetSitesBounds();

            var sqrtNsites = (int) Math.Sqrt(_sites.Count + 4);
            var heap = new HalfEdgePriorityQueue(dataBounds.Y, dataBounds.Height, sqrtNsites);
            var edgeList = new EdgeList(dataBounds.X, dataBounds.Width, sqrtNsites);
            var halfEdges = new List<HalfEdge>();
            var vertices = new List<Vertex>();

            _fortunesAlgorithmBottomMostSite = _sites.Next();
            var newSite = _sites.Next();

            for (;;)
            {
                if (heap.Empty() == false)
                {
                    newintstar = heap.Min();
                }

                Site bottomSite;
                HalfEdge lbnd;
                HalfEdge rbnd;
                HalfEdge bisector;
                if (newSite != null
                    && (heap.Empty() || CompareByYThenX(newSite, newintstar) < 0))
                {
                    /* new site is smallest */
                    //trace("smallest: new site " + newSite);

                    // Step 8:
                    lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord); // the Halfedge just to the left of newSite
                    //trace("lbnd: " + lbnd);
                    rbnd = lbnd.EdgeListRightNeighbor; // the Halfedge just to the right
                    //trace("rbnd: " + rbnd);
                    bottomSite = FortunesAlgorithm_rightRegion(lbnd); // this is the same as leftRegion(rbnd)
                    // this Site determines the region containing the new site
                    //trace("new Site is in region of existing site: " + bottomSite);

                    // Step 9:
                    edge = Edge.CreateBisectingEdge(bottomSite, newSite);
                    //trace("new edge: " + edge);
                    _edges.Add(edge);

                    bisector = HalfEdge.Create(edge, Side.Left);
                    halfEdges.Add(bisector);
                    // inserting two Halfedges into edgeList constitutes Step 10:
                    // insert bisector to the right of lbnd:
                    edgeList.Insert(lbnd, bisector);

                    // first half of Step 11:
                    if ((vertex = Vertex.Intersect(lbnd, bisector)) != null)
                    {
                        vertices.Add(vertex);
                        heap.Remove(lbnd);
                        lbnd.Vertex = vertex;
                        lbnd.Ystar = vertex.Y + newSite.Dist(vertex);
                        heap.Insert(lbnd);
                    }

                    lbnd = bisector;
                    bisector = HalfEdge.Create(edge, Side.Right);
                    halfEdges.Add(bisector);
                    // second Halfedge for Step 10:
                    // insert bisector to the right of lbnd:
                    edgeList.Insert(lbnd, bisector);

                    // second half of Step 11:
                    if ((vertex = Vertex.Intersect(bisector, rbnd)) != null)
                    {
                        vertices.Add(vertex);
                        bisector.Vertex = vertex;
                        bisector.Ystar = vertex.Y + newSite.Dist(vertex);
                        heap.Insert(bisector);
                    }

                    newSite = _sites.Next();
                }
                else if (heap.Empty() == false)
                {
                    /* intersection is smallest */
                    lbnd = heap.ExtractMin();
                    var llbnd = lbnd.EdgeListLeftNeighbor;
                    rbnd = lbnd.EdgeListRightNeighbor;
                    var rrbnd = rbnd.EdgeListRightNeighbor;
                    bottomSite = FortunesAlgorithm_leftRegion(lbnd);
                    var topSite = FortunesAlgorithm_rightRegion(rbnd);
                    // these three sites define a Delaunay triangle
                    // (not actually using these for anything...)
                    //_triangles.push(new Triangle(bottomSite, topSite, rightRegion(lbnd)));

                    var v = lbnd.Vertex;
                    v.SetIndex();
                    lbnd.Edge.SetVertex((Side) lbnd.LeftRight, v);
                    rbnd.Edge.SetVertex((Side) rbnd.LeftRight, v);
                    edgeList.Remove(lbnd);
                    heap.Remove(rbnd);
                    edgeList.Remove(rbnd);
                    var leftRight = Side.Left;
                    if (bottomSite.Y > topSite.Y)
                    {
                        var tempSite = bottomSite;
                        bottomSite = topSite;
                        topSite = tempSite;
                        leftRight = Side.Right;
                    }
                    edge = Edge.CreateBisectingEdge(bottomSite, topSite);
                    _edges.Add(edge);
                    bisector = HalfEdge.Create(edge, leftRight);
                    halfEdges.Add(bisector);
                    edgeList.Insert(llbnd, bisector);
                    edge.SetVertex(SideHelper.Other(leftRight), v);
                    if ((vertex = Vertex.Intersect(llbnd, bisector)) != null)
                    {
                        vertices.Add(vertex);
                        heap.Remove(llbnd);
                        llbnd.Vertex = vertex;
                        llbnd.Ystar = vertex.Y + bottomSite.Dist(vertex);
                        heap.Insert(llbnd);
                    }
                    if ((vertex = Vertex.Intersect(bisector, rrbnd)) != null)
                    {
                        vertices.Add(vertex);
                        bisector.Vertex = vertex;
                        bisector.Ystar = vertex.Y + bottomSite.Dist(vertex);
                        heap.Insert(bisector);
                    }
                }
                else
                {
                    break;
                }
            }

            // heap should be empty now
            heap.Dispose();
            edgeList.Dispose();

            foreach (var halfEdge in halfEdges)
            {
                halfEdge.ReallyDispose();
            }
            halfEdges.Clear();

            // we need the vertices to clip the edges
            foreach (Edge t in _edges)
            {
                edge = t;
                edge.ClipVertices(PlotBounds);
            }
            // but we don't actually ever use them again!
            foreach (Vertex t in vertices)
            {
                vertex = t;
                vertex.Dispose();
            }
            vertices.Clear();
        }

        private Site FortunesAlgorithm_leftRegion(HalfEdge he)
        {
            var edge = he.Edge;
            if (edge == null)
            {
                return _fortunesAlgorithmBottomMostSite;
            }
            return edge.Site((Side) he.LeftRight);
        }

        private Site FortunesAlgorithm_rightRegion(HalfEdge he)
        {
            var edge = he.Edge;
            if (edge == null)
            {
                return _fortunesAlgorithmBottomMostSite;
            }
            return edge.Site(SideHelper.Other((Side) he.LeftRight));
        }

        public static int CompareByYThenX(Site s1, Site s2)
        {
            if (s1.Y < s2.Y)
                return -1;
            if (s1.Y > s2.Y)
                return 1;
            if (s1.X < s2.X)
                return -1;
            if (s1.X > s2.X)
                return 1;
            return 0;
        }

        public static int CompareByYThenX(Site s1, Point s2)
        {
            if (s1.Y < s2.Y)
                return -1;
            if (s1.Y > s2.Y)
                return 1;
            if (s1.X < s2.X)
                return -1;
            if (s1.X > s2.X)
                return 1;
            return 0;
        }
    }
}