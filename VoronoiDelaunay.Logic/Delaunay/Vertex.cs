using System.Collections.Generic;
using System.Windows;
using VoronoiDelaunay.Logic.Delaunay.LR;

namespace VoronoiDelaunay.Logic.Delaunay
{
    public sealed class Vertex
        : ICoord
    {
        public static readonly Vertex VertexAtInfinity = new Vertex(double.NaN, double.NaN);

        private static readonly Stack<Vertex> Pool = new Stack<Vertex>();


        private static int _nvertices;

        public Vertex(double x, double y)
        {
            Init(x, y);
        }

        public int VertexIndex { get; private set; }

        public double X
        {
            get { return Coord.X; }
        }

        public double Y
        {
            get { return Coord.Y; }
        }

        public Point Coord { get; private set; }

        private static Vertex Create(double x, double y)
        {
            if (double.IsNaN(x) || double.IsNaN(y))
            {
                return VertexAtInfinity;
            }
            if (Pool.Count > 0)
            {
                return Pool.Pop().Init(x, y);
            }
            return new Vertex(x, y);
        }

        private Vertex Init(double x, double y)
        {
            Coord = new Point(x, y);
            return this;
        }

        public void Dispose()
        {
            Pool.Push(this);
        }

        public void SetIndex()
        {
            VertexIndex = _nvertices++;
        }

        public override string ToString()
        {
            return "Vertex (" + VertexIndex + ")";
        }

        /**
         * This is the only way to make a Vertex
         * 
         * @param halfedge0
         * @param halfedge1
         * @return 
         * 
         */

        public static Vertex Intersect(HalfEdge halfedge0, HalfEdge halfedge1)
        {
            Edge edge;
            HalfEdge halfEdge;

            var edge0 = halfedge0.Edge;
            var edge1 = halfedge1.Edge;
            if (edge0 == null || edge1 == null)
            {
                return null;
            }
            if (edge0.RightSite == edge1.RightSite)
            {
                return null;
            }

            var determinant = edge0.A*edge1.B - edge0.B*edge1.A;
            if (-1.0e-10 < determinant && determinant < 1.0e-10)
            {
                // the edges are parallel
                return null;
            }

            var intersectionX = (edge0.C*edge1.B - edge1.C*edge0.B)/determinant;
            var intersectionY = (edge1.C*edge0.A - edge0.C*edge1.A)/determinant;

            if (Voronoi.CompareByYThenX(edge0.RightSite, edge1.RightSite) < 0)
            {
                halfEdge = halfedge0;
                edge = edge0;
            }
            else
            {
                halfEdge = halfedge1;
                edge = edge1;
            }
            var rightOfSite = intersectionX >= edge.RightSite.X;
            if ((rightOfSite && halfEdge.LeftRight == Side.Left)
                || (!rightOfSite && halfEdge.LeftRight == Side.Right))
            {
                return null;
            }

            return Create(intersectionX, intersectionY);
        }
    }
}