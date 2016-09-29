using System;
using System.Collections.Generic;
using System.Windows;
using VoronoiDelaunay.Logic.Delaunay.LR;

namespace VoronoiDelaunay.Logic.Delaunay
{
    public sealed class HalfEdge
        : IDisposable
    {
        private static readonly Stack<HalfEdge> Pool = new Stack<HalfEdge>();

        public Edge Edge;

        public HalfEdge EdgeListLeftNeighbor, EdgeListRightNeighbor;
        public Side? LeftRight;
        public HalfEdge NextInPriorityQueue;
        public Vertex Vertex;

        // the vertex's y-coordinate in the transformed Voronoi space V*
        public double Ystar;

        public HalfEdge(Edge edge = null, Side? lr = null)
        {
            Init(edge, lr);
        }

        public void Dispose()
        {
            if (EdgeListLeftNeighbor != null || EdgeListRightNeighbor != null)
            {
                // still in EdgeList
                return;
            }
            if (NextInPriorityQueue != null)
            {
                // still in PriorityQueue
                return;
            }
            Edge = null;
            LeftRight = null;
            Vertex = null;
            Pool.Push(this);
        }

        public static HalfEdge Create(Edge edge, Side? lr)
        {
            if (Pool.Count > 0)
            {
                return Pool.Pop().Init(edge, lr);
            }

            return new HalfEdge(edge, lr);
        }

        public static HalfEdge CreateDummy()
        {
            return Create(null, null);
        }

        private HalfEdge Init(Edge edge, Side? lr)
        {
            Edge = edge;
            LeftRight = lr;
            NextInPriorityQueue = null;
            Vertex = null;
            return this;
        }

        public override string ToString()
        {
            return "Halfedge (leftRight: " + LeftRight + "; vertex: " + Vertex + ")";
        }

        public void ReallyDispose()
        {
            EdgeListLeftNeighbor = null;
            EdgeListRightNeighbor = null;
            NextInPriorityQueue = null;
            Edge = null;
            LeftRight = null;
            Vertex = null;
            Pool.Push(this);
        }

        internal bool IsLeftOf(Point p)
        {
            bool above;

            var topSite = Edge.RightSite;
            var rightOfSite = p.X > topSite.X;
            if (rightOfSite && LeftRight == Side.Left)
            {
                return true;
            }
            if (!rightOfSite && LeftRight == Side.Right)
            {
                return false;
            }

            if (Edge.A == 1.0)
            {
                var dyp = p.Y - topSite.Y;
                var dxp = p.X - topSite.X;
                var fast = false;
                if ((!rightOfSite && Edge.B < 0.0) || (rightOfSite && Edge.B >= 0.0))
                {
                    above = dyp >= Edge.B*dxp;
                    fast = above;
                }
                else
                {
                    above = p.X + p.Y*Edge.B > Edge.C;
                    if (Edge.B < 0.0)
                    {
                        above = !above;
                    }
                    if (!above)
                    {
                        fast = true;
                    }
                }
                if (!fast)
                {
                    var dxs = topSite.X - Edge.LeftSite.X;
                    above = Edge.B*(dxp*dxp - dyp*dyp) <
                            dxs*dyp*(1.0 + 2.0*dxp/dxs + Edge.B*Edge.B);
                    if (Edge.B < 0.0)
                    {
                        above = !above;
                    }
                }
            }
            else
            {
                /* edge.b == 1.0 */
                var yl = Edge.C - Edge.A*p.X;
                var t1 = p.Y - yl;
                var t2 = p.X - topSite.X;
                var t3 = yl - topSite.Y;
                above = t1*t1 > t2*t2 + t3*t3;
            }
            return LeftRight == Side.Left ? above : !above;
        }
    }
}