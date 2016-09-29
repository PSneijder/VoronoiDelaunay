using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using VoronoiDelaunay.Logic.Delaunay.LR;
using VoronoiDelaunay.Logic.Geo;

namespace VoronoiDelaunay.Logic.Delaunay
{
    public sealed class Site
        : ICoord, IComparable
    {
        private static readonly Stack<Site> Pool = new Stack<Site>();


        private static readonly double EPSILON = .005f;

        /**
         which end of each edge hooks up with the previous edge in _edges:
		 
         This MUST BE exposed - it is absurd to hide this, without it the Site
         is generating corrupt data (the .edges property is meaningless without
         access to this list)
         */

        // the edges that define this Site's Voronoi region:
        // ordered list of points that define the region clipped to bounds:
        private List<Point> _region;

        private uint _siteIndex;

        public Color Color;
        public double Weight;

        private Site(Point p, uint index, double weight, Color color)
        {
            //			if (lock != PrivateConstructorEnforcer)
            //			{
            //				throw new Error("Site constructor is private");
            //			}
            Init(p, index, weight, color);
        }

        internal List<Edge> Edges { get; private set; }

        public List<Side> EdgeOrientations { get; private set; }

        public double X
        {
            get { return Coord.X; }
        }

        internal double Y
        {
            get { return Coord.Y; }
        }

        /**
         * sort sites on y, then x, coord
         * also change each site's _siteIndex to match its new position in the list
         * so the _siteIndex can be used to identify the site for nearest-neighbor queries
         * 
         * haha "also" - means more than one responsibility...
         * 
         */

        public int CompareTo(object obj)
            // XXX: Really, really worried about this because it depends on how sorting works in AS3 impl - Julian
        {
            var s2 = (Site) obj;

            var returnValue = Voronoi.CompareByYThenX(this, s2);

            // swap _siteIndex values if necessary to match new ordering:
            uint tempIndex;
            if (returnValue == -1)
            {
                if (_siteIndex > s2._siteIndex)
                {
                    tempIndex = _siteIndex;
                    _siteIndex = s2._siteIndex;
                    s2._siteIndex = tempIndex;
                }
            }
            else if (returnValue == 1)
            {
                if (s2._siteIndex > _siteIndex)
                {
                    tempIndex = s2._siteIndex;
                    s2._siteIndex = _siteIndex;
                    _siteIndex = tempIndex;
                }
            }

            return returnValue;
        }

        public Point Coord { get; private set; }

        public static Site Create(Point p, uint index, double weight, Color color)
        {
            if (Pool.Count > 0)
            {
                return Pool.Pop().Init(p, index, weight, color);
            }
            return new Site(p, index, weight, color);
        }

        internal static void SortSites(List<Site> sites)
        {
            //			sites.sort(Site.compare);
            sites.Sort(); // XXX: Check if this works
        }

        /**
        This ABSOLUTELY has to be public! Otherwise you CANNOT workaround
        the major accuracy-bugs in the AS3Delaunay library (because it does NOT
        use stable, consistent data, sadly: you cannot compare two Point objects
        and get a correct answer to "isEqual", it corrupts them at a micro level :( )
        */

        public static bool CloseEnough(Point p0, Point p1)
        {
            return p0.Distance(p1) < EPSILON;
        }

        private Site Init(Point p, uint index, double weight, Color color)
        {
            Coord = p;
            _siteIndex = index;
            this.Weight = weight;
            this.Color = color;
            Edges = new List<Edge>();
            _region = null;
            return this;
        }

        public override string ToString()
        {
            return "Site " + _siteIndex + ": " + Coord;
        }

        private void Move(Point p)
        {
            Clear();
            Coord = p;
        }

        public void Dispose()
        {
            //			_coord = null;
            Clear();
            Pool.Push(this);
        }

        private void Clear()
        {
            if (Edges != null)
            {
                Edges.Clear();
                Edges = null;
            }
            if (EdgeOrientations != null)
            {
                EdgeOrientations.Clear();
                EdgeOrientations = null;
            }
            if (_region != null)
            {
                _region.Clear();
                _region = null;
            }
        }

        public void AddEdge(Edge edge)
        {
            Edges.Add(edge);
        }

        public Edge NearestEdge()
        {
            Edges.Sort(Edge.CompareSitesDistances);
            return Edges[0];
        }

        public List<Site> NeighborSites()
        {
            if (Edges == null || Edges.Count == 0)
            {
                return new List<Site>();
            }
            if (EdgeOrientations == null)
            {
                ReorderEdges();
            }
            var list = new List<Site>();
            Edge edge;
            for (var i = 0; i < Edges.Count; i++)
            {
                edge = Edges[i];
                list.Add(NeighborSite(edge));
            }
            return list;
        }

        private Site NeighborSite(Edge edge)
        {
            if (this == edge.LeftSite)
            {
                return edge.RightSite;
            }
            if (this == edge.RightSite)
            {
                return edge.LeftSite;
            }
            return null;
        }

        internal List<Point> Region(Rect clippingBounds)
        {
            if (Edges == null || Edges.Count == 0)
            {
                return new List<Point>();
            }
            if (EdgeOrientations == null)
            {
                ReorderEdges();
                _region = ClipToBounds(clippingBounds);
                if (new Polygon(_region).Winding() == Winding.Clockwise)
                {
                    _region.Reverse();
                }
            }
            return _region;
        }

        private void ReorderEdges()
        {
            //trace("_edges:", _edges);
            var reorderer = new EdgeReorderer(Edges, VertexOrSite.Vertex);
            Edges = reorderer.Edges;
            //trace("reordered:", _edges);
            EdgeOrientations = reorderer.EdgeOrientations;
            reorderer.Dispose();
        }

        private List<Point> ClipToBounds(Rect bounds)
        {
            var points = new List<Point>();
            var n = Edges.Count;
            var i = 0;
            Edge edge;
            while (i < n && (Edges[i].Visible == false))
            {
                ++i;
            }

            if (i == n)
            {
                // no edges visible
                return new List<Point>();
            }
            edge = Edges[i];
            var orientation = EdgeOrientations[i];

            if (edge.ClippedEnds[orientation] == null)
            {
                Debug.WriteLine("XXX: Null detected when there should be a Point!", "Error");
            }
            if (edge.ClippedEnds[SideHelper.Other(orientation)] == null)
            {
                Debug.WriteLine("XXX: Null detected when there should be a Point!", "Error");
            }
            points.Add((Point) edge.ClippedEnds[orientation]);
            points.Add((Point) edge.ClippedEnds[SideHelper.Other(orientation)]);

            for (var j = i + 1; j < n; ++j)
            {
                edge = Edges[j];
                if (edge.Visible == false)
                {
                    continue;
                }
                Connect(points, j, bounds);
            }
            // close up the polygon by adding another corner point of the bounds if needed:
            Connect(points, i, bounds, true);

            return points;
        }

        private void Connect(List<Point> points, int j, Rect bounds, bool closingUp = false)
        {
            var rightPoint = points[points.Count - 1];
            var newEdge = Edges[j];
            var newOrientation = EdgeOrientations[j];
            // the point that  must be connected to rightPoint:
            if (newEdge.ClippedEnds[newOrientation] == null)
            {
                Debug.WriteLine("XXX: Null detected when there should be a Point!", "Error");
            }
            var newPoint = (Point) newEdge.ClippedEnds[newOrientation];
            if (!CloseEnough(rightPoint, newPoint))
            {
                // The points do not coincide, so they must have been clipped at the bounds;
                // see if they are on the same border of the bounds:
                if (rightPoint.X != newPoint.X
                    && rightPoint.Y != newPoint.Y)
                {
                    // They are on different borders of the bounds;
                    // insert one or two corners of bounds as needed to hook them up:
                    // (NOTE this will not be correct if the region should take up more than
                    // half of the bounds rect, for then we will have gone the wrong way
                    // around the bounds and included the smaller part rather than the larger)
                    var rightCheck = BoundsCheck.Check(rightPoint, bounds);
                    var newCheck = BoundsCheck.Check(newPoint, bounds);
                    double px, py;
                    if ((rightCheck & BoundsCheck.Right) != 0)
                    {
                        px = bounds.Right;
                        if ((newCheck & BoundsCheck.Bottom) != 0)
                        {
                            py = bounds.Bottom;
                            points.Add(new Point(px, py));
                        }
                        else if ((newCheck & BoundsCheck.Top) != 0)
                        {
                            py = bounds.Top;
                            points.Add(new Point(px, py));
                        }
                        else if ((newCheck & BoundsCheck.Left) != 0)
                        {
                            if (rightPoint.Y - bounds.Y + newPoint.Y - bounds.Y < bounds.Height)
                            {
                                py = bounds.Top;
                            }
                            else
                            {
                                py = bounds.Bottom;
                            }
                            points.Add(new Point(px, py));
                            points.Add(new Point(bounds.Left, py));
                        }
                    }
                    else if ((rightCheck & BoundsCheck.Left) != 0)
                    {
                        px = bounds.Left;
                        if ((newCheck & BoundsCheck.Bottom) != 0)
                        {
                            py = bounds.Bottom;
                            points.Add(new Point(px, py));
                        }
                        else if ((newCheck & BoundsCheck.Top) != 0)
                        {
                            py = bounds.Top;
                            points.Add(new Point(px, py));
                        }
                        else if ((newCheck & BoundsCheck.Right) != 0)
                        {
                            if (rightPoint.Y - bounds.Y + newPoint.Y - bounds.Y < bounds.Height)
                            {
                                py = bounds.Top;
                            }
                            else
                            {
                                py = bounds.Bottom;
                            }
                            points.Add(new Point(px, py));
                            points.Add(new Point(bounds.Right, py));
                        }
                    }
                    else if ((rightCheck & BoundsCheck.Top) != 0)
                    {
                        py = bounds.Top;
                        if ((newCheck & BoundsCheck.Right) != 0)
                        {
                            px = bounds.Right;
                            points.Add(new Point(px, py));
                        }
                        else if ((newCheck & BoundsCheck.Left) != 0)
                        {
                            px = bounds.Left;
                            points.Add(new Point(px, py));
                        }
                        else if ((newCheck & BoundsCheck.Bottom) != 0)
                        {
                            if (rightPoint.X - bounds.X + newPoint.X - bounds.X < bounds.Width)
                            {
                                px = bounds.Left;
                            }
                            else
                            {
                                px = bounds.Right;
                            }
                            points.Add(new Point(px, py));
                            points.Add(new Point(px, bounds.Bottom));
                        }
                    }
                    else if ((rightCheck & BoundsCheck.Bottom) != 0)
                    {
                        py = bounds.Bottom;
                        if ((newCheck & BoundsCheck.Right) != 0)
                        {
                            px = bounds.Right;
                            points.Add(new Point(px, py));
                        }
                        else if ((newCheck & BoundsCheck.Left) != 0)
                        {
                            px = bounds.Left;
                            points.Add(new Point(px, py));
                        }
                        else if ((newCheck & BoundsCheck.Top) != 0)
                        {
                            if (rightPoint.X - bounds.X + newPoint.X - bounds.X < bounds.Width)
                            {
                                px = bounds.Left;
                            }
                            else
                            {
                                px = bounds.Right;
                            }
                            points.Add(new Point(px, py));
                            points.Add(new Point(px, bounds.Top));
                        }
                    }
                }
                if (closingUp)
                {
                    // newEdge's ends have already been added
                    return;
                }
                points.Add(newPoint);
            }
            if (newEdge.ClippedEnds[SideHelper.Other(newOrientation)] == null)
            {
                Debug.WriteLine("XXX: Null detected when there should be a Point!", "Error");
            }
            var newRightPoint = (Point) newEdge.ClippedEnds[SideHelper.Other(newOrientation)];
            if (!CloseEnough(points[0], newRightPoint))
            {
                points.Add(newRightPoint);
            }
        }

        public double Dist(ICoord p)
        {
            return p.Coord.Distance(Coord);
        }
    }

//	class PrivateConstructorEnforcer {}

//	import flash.geom.Point;
//	import flash.geom.Rectangle;

    internal static class BoundsCheck
    {
        public static readonly int Top = 1;
        public static readonly int Bottom = 2;
        public static readonly int Left = 4;
        public static readonly int Right = 8;

        /**
         * 
         * @param point
         * @param bounds
         * @return an int with the appropriate bits set if the Point lies on the corresponding bounds lines
         * 
         */

        public static int Check(Point point, Rect bounds)
        {
            var value = 0;
            if (point.X == bounds.Left)
            {
                value |= Left;
            }
            if (point.X == bounds.Right)
            {
                value |= Right;
            }
            if (point.Y == bounds.Top)
            {
                value |= Top;
            }
            if (point.Y == bounds.Bottom)
            {
                value |= Bottom;
            }
            return value;
        }
    }
}