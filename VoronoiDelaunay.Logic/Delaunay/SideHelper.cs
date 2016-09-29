namespace VoronoiDelaunay.Logic.Delaunay
{
    namespace LR
    {
        public enum Side
        {
            Left = 0,
            Right
        }

        public class SideHelper
        {
            public static Side Other(Side leftRight)
            {
                return leftRight == Side.Left ? Side.Right : Side.Left;
            }
        }
    }
}