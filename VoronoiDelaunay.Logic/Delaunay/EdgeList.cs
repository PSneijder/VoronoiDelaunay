using System;
using System.Windows;

namespace VoronoiDelaunay.Logic.Delaunay
{
    internal sealed class EdgeList : IDisposable
    {
        private readonly double _deltax;

        private readonly int _hashsize;
        private readonly double _xmin;
        private HalfEdge[] _hash;

        public EdgeList(double xmin, double deltax, int sqrtNsites)
        {
            _xmin = xmin;
            _deltax = deltax;
            _hashsize = 2*sqrtNsites;

            _hash = new HalfEdge[_hashsize];

            // two dummy Halfedges:
            LeftEnd = HalfEdge.CreateDummy();
            RightEnd = HalfEdge.CreateDummy();
            LeftEnd.EdgeListLeftNeighbor = null;
            LeftEnd.EdgeListRightNeighbor = RightEnd;
            RightEnd.EdgeListLeftNeighbor = LeftEnd;
            RightEnd.EdgeListRightNeighbor = null;
            _hash[0] = LeftEnd;
            _hash[_hashsize - 1] = RightEnd;
        }

        public HalfEdge LeftEnd { get; private set; }

        public HalfEdge RightEnd { get; private set; }

        public void Dispose()
        {
            var halfEdge = LeftEnd;
            HalfEdge prevHe;
            while (halfEdge != RightEnd)
            {
                prevHe = halfEdge;
                halfEdge = halfEdge.EdgeListRightNeighbor;
                prevHe.Dispose();
            }
            LeftEnd = null;
            RightEnd.Dispose();
            RightEnd = null;

            int i;
            for (i = 0; i < _hashsize; ++i)
            {
                _hash[i] = null;
            }
            _hash = null;
        }

        /**
		 * Insert newHalfedge to the right of lb 
		 * @param lb
		 * @param newHalfedge
		 * 
		 */

        public void Insert(HalfEdge lb, HalfEdge newHalfEdge)
        {
            newHalfEdge.EdgeListLeftNeighbor = lb;
            newHalfEdge.EdgeListRightNeighbor = lb.EdgeListRightNeighbor;
            lb.EdgeListRightNeighbor.EdgeListLeftNeighbor = newHalfEdge;
            lb.EdgeListRightNeighbor = newHalfEdge;
        }

        /**
		 * This function only removes the Halfedge from the left-right list.
		 * We cannot dispose it yet because we are still using it. 
		 * @param halfEdge
		 * 
		 */

        public void Remove(HalfEdge halfEdge)
        {
            halfEdge.EdgeListLeftNeighbor.EdgeListRightNeighbor = halfEdge.EdgeListRightNeighbor;
            halfEdge.EdgeListRightNeighbor.EdgeListLeftNeighbor = halfEdge.EdgeListLeftNeighbor;
            halfEdge.Edge = Edge.Deleted;
            halfEdge.EdgeListLeftNeighbor = halfEdge.EdgeListRightNeighbor = null;
        }

        /**
		 * Find the rightmost Halfedge that is still left of p 
		 * @param p
		 * @return 
		 * 
		 */

        public HalfEdge EdgeListLeftNeighbor(Point p)
        {
            /* Use hash table to get close to desired halfedge */
            var bucket = (int) ((p.X - _xmin)/_deltax*_hashsize);
            if (bucket < 0)
            {
                bucket = 0;
            }
            if (bucket >= _hashsize)
            {
                bucket = _hashsize - 1;
            }
            var halfEdge = GetHash(bucket);
            if (halfEdge == null)
            {
                int i;
                for (i = 1;; ++i)
                {
                    if ((halfEdge = GetHash(bucket - i)) != null)
                        break;
                    if ((halfEdge = GetHash(bucket + i)) != null)
                        break;
                }
            }
            /* Now search linear list of halfedges for the correct one */
            if (halfEdge == LeftEnd || (halfEdge != RightEnd && halfEdge.IsLeftOf(p)))
            {
                do
                {
                    halfEdge = halfEdge.EdgeListRightNeighbor;
                } while (halfEdge != RightEnd && halfEdge.IsLeftOf(p));
                halfEdge = halfEdge.EdgeListLeftNeighbor;
            }
            else
            {
                do
                {
                    halfEdge = halfEdge.EdgeListLeftNeighbor;
                } while (halfEdge != LeftEnd && !halfEdge.IsLeftOf(p));
            }

            /* Update hash table and reference counts */
            if (bucket > 0 && bucket < _hashsize - 1)
            {
                _hash[bucket] = halfEdge;
            }
            return halfEdge;
        }

        /* Get entry from hash table, pruning any deleted nodes */

        private HalfEdge GetHash(int b)
        {
            HalfEdge halfEdge;

            if (b < 0 || b >= _hashsize)
            {
                return null;
            }
            halfEdge = _hash[b];
            if (halfEdge != null && halfEdge.Edge == Edge.Deleted)
            {
                /* Hash table points to deleted halfedge.  Patch as necessary. */
                _hash[b] = null;
                // still can't dispose halfEdge yet!
                return null;
            }
            return halfEdge;
        }
    }
}