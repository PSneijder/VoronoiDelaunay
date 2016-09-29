using System;
using System.Collections.Generic;
using System.Windows;
using VoronoiDelaunay.Logic.Delaunay.LR;
using VoronoiDelaunay.Logic.Geo;

namespace VoronoiDelaunay.Logic.Delaunay
{
    public class Node
    {
        public static Stack<Node> Pool = new Stack<Node>();

        public Node Parent;
        public int TreeSize;
    }

    public enum KruskalType
    {
        Minimum,
        Maximum
    }

    public static class DelaunayHelpers
    {
        public static double Distance(this Point p1, Point p2)
        {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return Math.Sqrt(dx*dx + dy*dy);
        }

        public static List<LineSegment> VisibleLineSegments(List<Edge> edges)
        {
            var segments = new List<LineSegment>();

            foreach (var edge in edges)
            {
                if (edge.Visible)
                {
                    var p1 = edge.ClippedEnds[Side.Left];
                    var p2 = edge.ClippedEnds[Side.Right];
                    segments.Add(new LineSegment(p1, p2));
                }
            }

            return segments;
        }

        public static List<Edge> SelectEdgesForSitePoint(Point coord, List<Edge> edgesToTest)
        {
            return edgesToTest.FindAll(edge => (edge.LeftSite != null && edge.LeftSite.Coord == coord) || (edge.RightSite != null && edge.RightSite.Coord == coord));
        }

        public static List<Edge> SelectNonIntersectingEdges( /*keepOutMask:BitmapData,*/ List<Edge> edgesToTest)
        {
            //			if (keepOutMask == null)
            //			{
            return edgesToTest;
            //			}

            //			var zeroPoint:Point = new Point();
            //			return edgesToTest.filter(myTest);
            //			
            //			function myTest(edge:Edge, index:int, vector:Vector.<Edge>):Boolean
            //			{
            //				var delaunayLineBmp:BitmapData = edge.makeDelaunayLineBmp();
            //				var notIntersecting:Boolean = !(keepOutMask.hitTest(zeroPoint, 1, delaunayLineBmp, zeroPoint, 1));
            //				delaunayLineBmp.dispose();
            //				return notIntersecting;
            //			}
        }

        public static List<LineSegment> DelaunayLinesForEdges(List<Edge> edges)
        {
            var segments = new List<LineSegment>();

            foreach (var edge in edges)
            {
                segments.Add(edge.DelaunayLine());
            }

            return segments;
        }

        /**
        *  Kruskal's spanning tree algorithm with union-find
         * Skiena: The Algorithm Design Manual, p. 196ff
         * Note: the sites are implied: they consist of the end points of the line segments
        */

        public static List<LineSegment> Kruskal(List<LineSegment> lineSegments, KruskalType type = KruskalType.Minimum)
        {
            var nodes = new Dictionary<Point?, Node>();
            var mst = new List<LineSegment>();
            var nodePool = Node.Pool;

            switch (type)
            {
                // note that the compare functions are the reverse of what you'd expect
                // because (see below) we traverse the lineSegments in reverse order for speed
                case KruskalType.Maximum:
                    lineSegments.Sort(LineSegment.CompareLengths);
                    break;
                default:
                    lineSegments.Sort(LineSegment.CompareLengthsMax);
                    break;
            }

            for (var i = lineSegments.Count; --i > -1;)
            {
                var lineSegment = lineSegments[i];

                Node node0;
                Node rootOfSet0;
                if (!nodes.ContainsKey(lineSegment.P0))
                {
                    node0 = nodePool.Count > 0 ? nodePool.Pop() : new Node();
                    // intialize the node:
                    rootOfSet0 = node0.Parent = node0;
                    node0.TreeSize = 1;

                    nodes[lineSegment.P0] = node0;
                }
                else
                {
                    node0 = nodes[lineSegment.P0];
                    rootOfSet0 = Find(node0);
                }

                Node node1;
                Node rootOfSet1;
                if (!nodes.ContainsKey(lineSegment.P1))
                {
                    node1 = nodePool.Count > 0 ? nodePool.Pop() : new Node();
                    // intialize the node:
                    rootOfSet1 = node1.Parent = node1;
                    node1.TreeSize = 1;

                    nodes[lineSegment.P1] = node1;
                }
                else
                {
                    node1 = nodes[lineSegment.P1];
                    rootOfSet1 = Find(node1);
                }

                if (rootOfSet0 != rootOfSet1)
                {
                    // nodes not in same set
                    mst.Add(lineSegment);

                    // merge the two sets:
                    var treeSize0 = rootOfSet0.TreeSize;
                    var treeSize1 = rootOfSet1.TreeSize;
                    if (treeSize0 >= treeSize1)
                    {
                        // set0 absorbs set1:
                        rootOfSet1.Parent = rootOfSet0;
                        rootOfSet0.TreeSize += treeSize1;
                    }
                    else
                    {
                        // set1 absorbs set0:
                        rootOfSet0.Parent = rootOfSet1;
                        rootOfSet1.TreeSize += treeSize0;
                    }
                }
            }

            foreach (var node in nodes.Values)
            {
                nodePool.Push(node);
            }

            return mst;
        }

        private static Node Find(Node node)
        {
            if (node.Parent == node)
            {
                return node;
            }

            var root = Find(node.Parent);

            // this line is just to speed up subsequent finds by keeping the tree depth low:
            node.Parent = root;
            return root;
        }
    }
}