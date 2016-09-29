using System;
using System.Collections.Generic;
using VoronoiDelaunay.Logic.Delaunay.LR;

/** This class is horrible, and ought to be nuked from orbit. But the library is
heavily dependent upon it in undocumented ways.

It's viciously complicated, and is used all over the library in odd places where it
shouldn't be used, with no explanation - but with a hard dependency in that it
doesn't merely "re-order" edges (as the name suggests!) but often "generates" them
too.

It feels like it was intended to be semi-optimized (in the original AS3? probably),
but in a modern language like C#, there are far far better ways of doing this.

Currently: in my own projects, I am DELETING the output of this class, it's far
too dangerous to use in production. I recommend you do the same: write an
equivalent class (or better: set of classes) that are C#-friendly and do what they
say, and no more and no less. Hopefully one day someone will re-write this thing
and REMOVE IT from the rest of the library (all the places where it shouldn't be used)
*/

namespace VoronoiDelaunay.Logic.Delaunay
{
    public enum VertexOrSite
    {
        Vertex,
        Site
    }

    internal sealed class EdgeReorderer
        : IDisposable
    {
        public EdgeReorderer(List<Edge> origEdges, VertexOrSite criterion)
        {
            Edges = new List<Edge>();
            EdgeOrientations = new List<Side>();
            if (origEdges.Count > 0)
            {
                Edges = ReorderEdges(origEdges, criterion);
            }
        }

        public List<Edge> Edges { get; private set; }

        public List<Side> EdgeOrientations { get; private set; }

        public void Dispose()
        {
            Edges = null;
            EdgeOrientations = null;
        }

        private List<Edge> ReorderEdges(List<Edge> origEdges, VertexOrSite criterion)
        {
            var n = origEdges.Count;
            // we're going to reorder the edges in order of traversal
            var done = new bool[n];
            var nDone = 0;
            for (var j = 0; j < n; j++)
            {
                done[j] = false;
            }
            var newEdges = new List<Edge>(); // TODO: Switch to Deque if performance is a concern

            var i = 0;
            var edge = origEdges[i];
            newEdges.Add(edge);
            EdgeOrientations.Add(Side.Left);
            var firstPoint = criterion == VertexOrSite.Vertex ? edge.LeftVertex : (ICoord) edge.LeftSite;
            var lastPoint = criterion == VertexOrSite.Vertex ? edge.RightVertex : (ICoord) edge.RightSite;

            if (firstPoint == Vertex.VertexAtInfinity || lastPoint == Vertex.VertexAtInfinity)
            {
                return new List<Edge>();
            }

            done[i] = true;
            ++nDone;

            while (nDone < n)
            {
                for (i = 1; i < n; ++i)
                {
                    if (done[i])
                    {
                        continue;
                    }
                    edge = origEdges[i];
                    var leftPoint = criterion == VertexOrSite.Vertex ? edge.LeftVertex : (ICoord) edge.LeftSite;
                    var rightPoint = criterion == VertexOrSite.Vertex ? edge.RightVertex : (ICoord) edge.RightSite;
                    if (leftPoint == Vertex.VertexAtInfinity || rightPoint == Vertex.VertexAtInfinity)
                    {
                        return new List<Edge>();
                    }
                    if (leftPoint == lastPoint)
                    {
                        lastPoint = rightPoint;
                        EdgeOrientations.Add(Side.Left);
                        newEdges.Add(edge);
                        done[i] = true;
                    }
                    else if (rightPoint == firstPoint)
                    {
                        firstPoint = leftPoint;
                        EdgeOrientations.Insert(0, Side.Left); // TODO: Change datastructure if this is slow
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    }
                    else if (leftPoint == firstPoint)
                    {
                        firstPoint = rightPoint;
                        EdgeOrientations.Insert(0, Side.Right);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    }
                    else if (rightPoint == lastPoint)
                    {
                        lastPoint = leftPoint;
                        EdgeOrientations.Add(Side.Right);
                        newEdges.Add(edge);
                        done[i] = true;
                    }
                    if (done[i])
                    {
                        ++nDone;
                    }
                }
            }

            return newEdges;
        }
    }
}