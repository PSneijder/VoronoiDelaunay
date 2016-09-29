using System;
using System.Threading;

namespace VoronoiDelaunay.Utilities
{
    public static class StaticRandom
    {
        private static int _seed;
        private static readonly ThreadLocal<Random> ThreadLocal = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        static StaticRandom()
        {
            _seed = Environment.TickCount;
        }

        public static Random Instance { get { return ThreadLocal.Value; } }
    }
}