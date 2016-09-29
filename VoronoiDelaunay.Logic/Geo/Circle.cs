using System.Windows;

namespace VoronoiDelaunay.Logic.Geo
{
    public sealed class Circle
    {
        public Point Center;
        public double Radius;

        public Circle(double centerX, double centerY, double radius)
        {
            Center = new Point(centerX, centerY);
            this.Radius = radius;
        }

        public override string ToString()
        {
            return "Circle (center: " + Center + "; radius: " + Radius + ")";
        }
    }
}