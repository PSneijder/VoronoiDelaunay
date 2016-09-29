using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using VoronoiDelaunay.Logic.Geo;

namespace VoronoiDelaunay.Logic.Delaunay
{
    public sealed class SiteList
        : IDisposable
    {
        private int _currentIndex;
        private List<Site> _sites;

        private bool _sorted;

        public SiteList()
        {
            _sites = new List<Site>();
            _sorted = false;
        }

        public int Count
        {
            get { return _sites.Count; }
        }

        public void Dispose()
        {
            if (_sites != null)
            {
                foreach (var site in _sites)
                {
                    site.Dispose();
                }

                _sites.Clear();
                _sites = null;
            }
        }

        public int Add(Site site)
        {
            _sorted = false;
            _sites.Add(site);
            return _sites.Count;
        }

        public Site Next()
        {
            if (_sorted == false)
            {
                Debug.WriteLine("SiteList::next():  sites have not been sorted", "Error");
            }
            if (_currentIndex < _sites.Count)
            {
                return _sites[_currentIndex++];
            }
            return null;
        }

        internal Rect GetSitesBounds()
        {
            if (_sorted == false)
            {
                Site.SortSites(_sites);
                _currentIndex = 0;
                _sorted = true;
            }
            if (_sites.Count == 0)
            {
                return new Rect(0, 0, 0, 0);
            }
            var xmin = double.MaxValue;
            var xmax = double.MinValue;
            foreach (var site in _sites)
            {
                if (site.X < xmin)
                {
                    xmin = site.X;
                }
                if (site.X > xmax)
                {
                    xmax = site.X;
                }
            }
            // here's where we assume that the sites have been sorted on y:
            var ymin = _sites[0].Y;
            var ymax = _sites[_sites.Count - 1].Y;

            return new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
        }

        public List<Color> SiteColors( /*BitmapData referenceImage = null*/)
        {
            var colors = new List<Color>();
            foreach (var site in _sites)
            {
                colors.Add( /*referenceImage ? referenceImage.getPixel(site.x, site.y) :*/site.Color);
            }
            return colors;
        }

        public List<Point> SiteCoords()
        {
            var coords = new List<Point>();

            foreach (var site in _sites)
            {
                coords.Add(site.Coord);
            }

            return coords;
        }

        /**
         * 
         * @return the largest circle centered at each site that fits in its region;
         * if the region is infinite, return a circle of radius 0.
         * 
         */

        public List<Circle> Circles()
        {
            var circles = new List<Circle>();

            foreach (var site in _sites)
            {
                double radius = 0f;
                var nearestEdge = site.NearestEdge();

                if (!nearestEdge.IsPartOfConvexHull())
                {
                    radius = nearestEdge.SitesDistance()*0.5f;
                }
                circles.Add(new Circle(site.X, site.Y, radius));
            }

            return circles;
        }

        public List<List<Point>> Regions(Rect plotBounds)
        {
            var regions = new List<List<Point>>();

            foreach (var site in _sites)
            {
                regions.Add(site.Region(plotBounds));
            }

            return regions;
        }

        /**
         * 
         * @param proximityMap a BitmapData whose regions are filled with the site index values; see PlanePointsCanvas::fillRegions()
         * @param x
         * @param y
         * @return coordinates of nearest Site to (x, y)
         * 
         */

        public Point? NearestSitePoint( /*proximityMap:BitmapData,*/ double x, double y)
        {
            //			uint index = proximityMap.getPixel(x, y);
            //			if (index > _sites.length - 1)
            //			{
            return null;
            //			}
            //			return _sites[index].coord;
        }
    }
}