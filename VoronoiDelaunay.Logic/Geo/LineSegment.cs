using System.Windows;
using VoronoiDelaunay.Logic.Delaunay;

namespace VoronoiDelaunay.Logic.Geo
{
    public sealed class LineSegment
    {
        public Point? P0;
        public Point? P1;

        public LineSegment(Point? p0, Point? p1)
        {
            P0 = p0;
            P1 = p1;
        }

        public static int CompareLengthsMax(LineSegment segment0, LineSegment segment1)
        {
            var length0 = ((Point) segment0.P0).Distance((Point) segment0.P1);
            var length1 = ((Point) segment1.P0).Distance((Point) segment1.P1);
            if (length0 < length1)
            {
                return 1;
            }
            if (length0 > length1)
            {
                return -1;
            }
            return 0;
        }

        public static int CompareLengths(LineSegment edge0, LineSegment edge1)
        {
            return -CompareLengthsMax(edge0, edge1);
        }
    }
}