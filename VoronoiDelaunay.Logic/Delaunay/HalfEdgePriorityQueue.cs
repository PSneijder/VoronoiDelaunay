using System;
using System.Windows;

namespace VoronoiDelaunay.Logic.Delaunay
{
    internal sealed class HalfEdgePriorityQueue
        : IDisposable // also known as heap
    {
        private readonly double _deltay;
        private readonly int _hashsize;

        private readonly double _ymin;
        private int _count;
        private HalfEdge[] _hash;
        private int _minBucket;

        public HalfEdgePriorityQueue(double ymin, double deltay, int sqrtNsites)
        {
            _ymin = ymin;
            _deltay = deltay;
            _hashsize = 4*sqrtNsites;
            Initialize();
        }

        public void Dispose()
        {
            // get rid of dummies
            for (var i = 0; i < _hashsize; ++i)
            {
                _hash[i].Dispose();
                _hash[i] = null;
            }
            _hash = null;
        }

        private void Initialize()
        {
            int i;

            _count = 0;
            _minBucket = 0;
            _hash = new HalfEdge[_hashsize];
            // dummy Halfedge at the top of each hash
            for (i = 0; i < _hashsize; ++i)
            {
                _hash[i] = HalfEdge.CreateDummy();
                _hash[i].NextInPriorityQueue = null;
            }
        }

        public void Insert(HalfEdge halfEdge)
        {
            HalfEdge previous, next;
            var insertionBucket = Bucket(halfEdge);
            if (insertionBucket < _minBucket)
            {
                _minBucket = insertionBucket;
            }
            previous = _hash[insertionBucket];
            while ((next = previous.NextInPriorityQueue) != null
                   &&
                   (halfEdge.Ystar > next.Ystar || (halfEdge.Ystar == next.Ystar && halfEdge.Vertex.X > next.Vertex.X)))
            {
                previous = next;
            }
            halfEdge.NextInPriorityQueue = previous.NextInPriorityQueue;
            previous.NextInPriorityQueue = halfEdge;
            ++_count;
        }

        public void Remove(HalfEdge halfEdge)
        {
            var removalBucket = Bucket(halfEdge);

            if (halfEdge.Vertex != null)
            {
                var previous = _hash[removalBucket];
                while (previous.NextInPriorityQueue != halfEdge)
                {
                    previous = previous.NextInPriorityQueue;
                }
                previous.NextInPriorityQueue = halfEdge.NextInPriorityQueue;
                _count--;
                halfEdge.Vertex = null;
                halfEdge.NextInPriorityQueue = null;
                halfEdge.Dispose();
            }
        }

        private int Bucket(HalfEdge halfEdge)
        {
            var theBucket = (int) ((halfEdge.Ystar - _ymin)/_deltay*_hashsize);
            if (theBucket < 0)
                theBucket = 0;
            if (theBucket >= _hashsize)
                theBucket = _hashsize - 1;
            return theBucket;
        }

        private bool IsEmpty(int bucket)
        {
            return _hash[bucket].NextInPriorityQueue == null;
        }

        /**
         * move _minBucket until it contains an actual Halfedge (not just the dummy at the top); 
         * 
         */

        private void AdjustMinBucket()
        {
            while (_minBucket < _hashsize - 1 && IsEmpty(_minBucket))
            {
                ++_minBucket;
            }
        }

        public bool Empty()
        {
            return _count == 0;
        }

        /**
         * @return coordinates of the Halfedge's vertex in V*, the transformed Voronoi diagram
         * 
         */

        public Point Min()
        {
            AdjustMinBucket();
            var answer = _hash[_minBucket].NextInPriorityQueue;
            return new Point(answer.Vertex.X, answer.Ystar);
        }

        /**
         * remove and return the min Halfedge
         * @return 
         * 
         */

        public HalfEdge ExtractMin()
        {
            // get the first real Halfedge in _minBucket
            var answer = _hash[_minBucket].NextInPriorityQueue;

            _hash[_minBucket].NextInPriorityQueue = answer.NextInPriorityQueue;
            _count--;
            answer.NextInPriorityQueue = null;

            return answer;
        }
    }
}