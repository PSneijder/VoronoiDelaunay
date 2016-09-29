using System;
using System.Collections.Generic;
using System.Windows;

namespace VoronoiDelaunay.Logic.Geo
{
    public sealed class Polygon
    {
        private readonly List<Point> _vertices;

        public Polygon(List<Point> vertices)
        {
            _vertices = vertices;
        }

        public double Area()
        {
            return Math.Abs(SignedDoubleArea()*0.5f);
            // XXX: I'm a bit nervous about this; not sure what the * 0.5 is for, bithacking?
        }

        public Winding Winding()
        {
            var signedDoubleArea = SignedDoubleArea();
            if (signedDoubleArea < 0)
            {
                return Geo.Winding.Clockwise;
            }
            if (signedDoubleArea > 0)
            {
                return Geo.Winding.Counterclockwise;
            }
            return Geo.Winding.None;
        }

        private double SignedDoubleArea()
            // XXX: I'm a bit nervous about this because Actionscript represents everything as doubles, not doubles 
        {
            int index;
            var n = _vertices.Count;
            double signedDoubleArea = 0; // Losing lots of precision?
            for (index = 0; index < n; ++index)
            {
                var nextIndex = (index + 1)%n;
                var point = _vertices[index];
                var next = _vertices[nextIndex];
                signedDoubleArea += point.X*next.Y - next.X*point.Y;
            }
            return signedDoubleArea;
        }
    }
}