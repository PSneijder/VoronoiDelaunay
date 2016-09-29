using System.Collections.Generic;
using System.Windows;
using VoronoiDelaunay.Logic.Delaunay.LR;
using VoronoiDelaunay.Logic.Geo;

namespace VoronoiDelaunay.Logic.Delaunay
{
    //	import com.nodename.geom.LineSegment;
    //	
    //	import flash.display.BitmapData;
    //	import flash.display.CapsStyle;
    //	import flash.display.Graphics;
    //	import flash.display.LineScaleMode;
    //	import flash.display.Sprite;
    //	import flash.geom.Point;
    //	import flash.geom.Rectangle;
    //	import flash.utils.Dictionary;

    /**
		 * The line segment connecting the two Sites is part of the Delaunay triangulation;
		 * the line segment connecting the two Vertices is part of the Voronoi diagram
		 * @author ashaw
		 * 
		 */

    public sealed class Edge
    {
        private static readonly Stack<Edge> Pool = new Stack<Edge>();

        private static int _nedges;

        public static readonly Edge Deleted = new Edge();

        // Once clipVertices() is called, this Dictionary will hold two Points
        // representing the clipped coordinates of the left and right ends...

        private readonly int _edgeIndex;

        // the two Voronoi vertices that the edge connects
        //		(if one of them is null, the edge extends to infinity)

        // the two input Sites for which this Edge is a bisector:
        private Dictionary<Side, Site> _sites;

        // the equation of the edge: ax + by = c
        public double A, B, C;

        private Edge()
        {
            //			if (lock != PrivateConstructorEnforcer)
            //			{
            //				throw new Error("Edge: constructor is private");
            //			}

            _edgeIndex = _nedges++;
            Init();
        }

        public Vertex LeftVertex { get; private set; }

        public Vertex RightVertex { get; private set; }

        public Dictionary<Side, Point?> ClippedEnds { get; private set; }

        // unless the entire Edge is outside the bounds.
        // In that case visible will be false:
        public bool Visible
        {
            get { return ClippedEnds != null; }
        }

        public Site LeftSite
        {
            get { return _sites[Side.Left]; }
            set { _sites[Side.Left] = value; }
        }

        public Site RightSite
        {
            get { return _sites[Side.Right]; }
            set { _sites[Side.Right] = value; }
        }

        /**
			 * This is the only way to create a new Edge 
			 * @param site0
			 * @param site1
			 * @return 
			 * 
			 */

        public static Edge CreateBisectingEdge(Site site0, Site site1)
        {
            double a, b;

            var dx = site1.X - site0.X;
            var dy = site1.Y - site0.Y;
            var absdx = dx > 0 ? dx : -dx;
            var absdy = dy > 0 ? dy : -dy;
            var c = site0.X*dx + site0.Y*dy + (dx*dx + dy*dy)*0.5f;
            if (absdx > absdy)
            {
                a = 1.0f;
                b = dy/dx;
                c /= dx;
            }
            else
            {
                b = 1.0f;
                a = dx/dy;
                c /= dy;
            }

            var edge = Create();

            edge.LeftSite = site0;
            edge.RightSite = site1;
            site0.AddEdge(edge);
            site1.AddEdge(edge);

            edge.LeftVertex = null;
            edge.RightVertex = null;

            edge.A = a;
            edge.B = b;
            edge.C = c;
            //trace("createBisectingEdge: a ", edge.a, "b", edge.b, "c", edge.c);

            return edge;
        }

        private static Edge Create()
        {
            Edge edge;
            if (Pool.Count > 0)
            {
                edge = Pool.Pop();
                edge.Init();
            }
            else
            {
                edge = new Edge();
            }
            return edge;
        }

        //		private static const LINESPRITE:Sprite = new Sprite();
        //		private static const GRAPHICS:Graphics = LINESPRITE.graphics;
        //		
        //		private var _delaunayLineBmp:BitmapData;
        //		internal function get delaunayLineBmp():BitmapData
        //		{
        //			if (!_delaunayLineBmp)
        //			{
        //				_delaunayLineBmp = makeDelaunayLineBmp();
        //			}
        //			return _delaunayLineBmp;
        //		}
        //		
        //		// making this available to Voronoi; running out of memory in AIR so I cannot cache the bmp
        //		internal function makeDelaunayLineBmp():BitmapData
        //		{
        //			var p0:Point = leftSite.coord;
        //			var p1:Point = rightSite.coord;
        //			
        //			GRAPHICS.clear();
        //			// clear() resets line style back to undefined!
        //			GRAPHICS.lineStyle(0, 0, 1.0, false, LineScaleMode.NONE, CapsStyle.NONE);
        //			GRAPHICS.moveTo(p0.x, p0.y);
        //			GRAPHICS.lineTo(p1.x, p1.y);
        //						
        //			var w:int = int(Math.ceil(Math.max(p0.x, p1.x)));
        //			if (w < 1)
        //			{
        //				w = 1;
        //			}
        //			var h:int = int(Math.ceil(Math.max(p0.y, p1.y)));
        //			if (h < 1)
        //			{
        //				h = 1;
        //			}
        //			var bmp:BitmapData = new BitmapData(w, h, true, 0);
        //			bmp.draw(LINESPRITE);
        //			return bmp;
        //			}

        public LineSegment DelaunayLine()
        {
            // draw a line connecting the input Sites for which the edge is a bisector:
            return new LineSegment(LeftSite.Coord, RightSite.Coord);
        }

        public LineSegment VoronoiEdge()
        {
            if (!Visible)
                return new LineSegment(null, null);

            return new LineSegment(ClippedEnds[Side.Left], ClippedEnds[Side.Right]);
        }

        public Vertex Vertex(Side leftRight)
        {
            return leftRight == Side.Left ? LeftVertex : RightVertex;
        }

        public void SetVertex(Side leftRight, Vertex v)
        {
            if (leftRight == Side.Left)
            {
                LeftVertex = v;
            }
            else
            {
                RightVertex = v;
            }
        }

        public bool IsPartOfConvexHull()
        {
            return LeftVertex == null || RightVertex == null;
        }

        public double SitesDistance()
        {
            return LeftSite.Coord.Distance(RightSite.Coord);
        }

        public static int CompareSitesDistances_MAX(Edge edge0, Edge edge1)
        {
            var length0 = edge0.SitesDistance();
            var length1 = edge1.SitesDistance();

            if (length0 < length1)
            {
                return 1;
            }

            if (length0 > length1)
            {
                return -1;
            }

            return 0;
        }

        public static int CompareSitesDistances(Edge edge0, Edge edge1)
        {
            return -CompareSitesDistances_MAX(edge0, edge1);
        }

        public Site Site(Side leftRight)
        {
            return _sites[leftRight];
        }

        public void Dispose()
        {
//			if (_delaunayLineBmp) {
//				_delaunayLineBmp.Dispose ();
//				_delaunayLineBmp = null;
//			}
            LeftVertex = null;
            RightVertex = null;

            if (ClippedEnds != null)
            {
                ClippedEnds[Side.Left] = null;
                ClippedEnds[Side.Right] = null;
                ClippedEnds = null;
            }

            _sites[Side.Left] = null;
            _sites[Side.Right] = null;
            _sites = null;

            Pool.Push(this);
        }

        private void Init()
        {
            _sites = new Dictionary<Side, Site>();
        }

        public override string ToString()
        {
            return "Edge " + _edgeIndex + "; sites " + _sites[Side.Left] + ", " + _sites[Side.Right]
                   + "; endVertices " + (LeftVertex != null ? LeftVertex.VertexIndex.ToString() : "null") + ", "
                   + (RightVertex != null ? RightVertex.VertexIndex.ToString() : "null") + "::";
        }

        /**
			 * Set _clippedVertices to contain the two ends of the portion of the Voronoi edge that is visible
			 * within the bounds.  If no part of the Edge falls within the bounds, leave _clippedVertices null. 
			 * @param bounds
			 * 
			 */

        public void ClipVertices(Rect bounds)
        {
            var xmin = bounds.Left;
            var ymin = bounds.Top;
            var xmax = bounds.Right;
            var ymax = bounds.Bottom;

            Vertex vertex0, vertex1;
            double x0, x1, y0, y1;

            if (A == 1.0 && B >= 0.0)
            {
                vertex0 = RightVertex;
                vertex1 = LeftVertex;
            }
            else
            {
                vertex0 = LeftVertex;
                vertex1 = RightVertex;
            }

            if (A == 1.0)
            {
                y0 = ymin;
                if (vertex0 != null && vertex0.Y > ymin)
                {
                    y0 = vertex0.Y;
                }
                if (y0 > ymax)
                {
                    return;
                }
                x0 = C - B*y0;

                y1 = ymax;
                if (vertex1 != null && vertex1.Y < ymax)
                {
                    y1 = vertex1.Y;
                }
                if (y1 < ymin)
                {
                    return;
                }
                x1 = C - B*y1;

                if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin))
                {
                    return;
                }

                if (x0 > xmax)
                {
                    x0 = xmax;
                    y0 = (C - x0)/B;
                }
                else if (x0 < xmin)
                {
                    x0 = xmin;
                    y0 = (C - x0)/B;
                }

                if (x1 > xmax)
                {
                    x1 = xmax;
                    y1 = (C - x1)/B;
                }
                else if (x1 < xmin)
                {
                    x1 = xmin;
                    y1 = (C - x1)/B;
                }
            }
            else
            {
                x0 = xmin;
                if (vertex0 != null && vertex0.X > xmin)
                {
                    x0 = vertex0.X;
                }
                if (x0 > xmax)
                {
                    return;
                }
                y0 = C - A*x0;

                x1 = xmax;
                if (vertex1 != null && vertex1.X < xmax)
                {
                    x1 = vertex1.X;
                }
                if (x1 < xmin)
                {
                    return;
                }
                y1 = C - A*x1;

                if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin))
                {
                    return;
                }

                if (y0 > ymax)
                {
                    y0 = ymax;
                    x0 = (C - y0)/A;
                }
                else if (y0 < ymin)
                {
                    y0 = ymin;
                    x0 = (C - y0)/A;
                }

                if (y1 > ymax)
                {
                    y1 = ymax;
                    x1 = (C - y1)/A;
                }
                else if (y1 < ymin)
                {
                    y1 = ymin;
                    x1 = (C - y1)/A;
                }
            }

            //			_clippedVertices = new Dictionary(true); // XXX: Weak ref'd dict might be a problem to use standard
            ClippedEnds = new Dictionary<Side, Point?>();
            if (vertex0 == LeftVertex)
            {
                ClippedEnds[Side.Left] = new Point(x0, y0);
                ClippedEnds[Side.Right] = new Point(x1, y1);
            }
            else
            {
                ClippedEnds[Side.Right] = new Point(x0, y0);
                ClippedEnds[Side.Left] = new Point(x1, y1);
            }
        }
    }
}