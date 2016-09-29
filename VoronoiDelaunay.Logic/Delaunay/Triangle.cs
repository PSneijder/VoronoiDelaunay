using System;
using System.Collections.Generic;

namespace VoronoiDelaunay.Logic.Delaunay
{
    public sealed class Triangle
        : IDisposable
    {
        public Triangle(Site a, Site b, Site c)
        {
            Sites = new List<Site> {a, b, c};
        }

        public List<Site> Sites { get; private set; }

        public void Dispose()
        {
            Sites.Clear();
            Sites = null;
        }
    }
}