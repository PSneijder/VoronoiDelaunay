using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VoronoiDelaunay.Annotations;
using VoronoiDelaunay.Logic.Delaunay;
using VoronoiDelaunay.Logic.Utilities;
using VoronoiDelaunay.Utilities;
using LineSegment = VoronoiDelaunay.Logic.Geo.LineSegment;
using Polygon = System.Windows.Shapes.Polygon;

namespace VoronoiDelaunay.ViewModels
{
    public sealed class MainWindowViewModel
        : INotifyPropertyChanged
    {
        #region Fields

        private readonly int MaxPoints = 60;
        private Voronoi _voronoi;

        #endregion

        #region Properties

        public ObservableCollection<Shape> ShapeSegments { get; set; }

        #endregion

        #region Commands

        public ICommand MouseClickedCommand { get; set; }

        #endregion

        public MainWindowViewModel()
        {
            ShapeSegments = new ObservableCollection<Shape>();
            MouseClickedCommand = new DelegateCommand(OnMouseClicked);

            CreateVoronoiDiagram();
        }

        #region Command Implementations

        private void OnMouseClicked(object parameter)
        {
            Point point = Mouse.GetPosition((IInputElement) parameter);

            CreateHighlightEffect(point.X, point.Y);
        }

        #endregion

        #region Private Methods

        private void CreateHighlightEffect(double x, double y)
        {
            Point? point = _voronoi.NearestSitePoint(x, y);

            if (point.HasValue)
            {
                List<LineSegment> boundaries = _voronoi.VoronoiBoundaryForSite(point.Value);
                IEnumerable<Line> lines = ShapeSegments.Where(segment => segment is Line).Cast<Line>();
            }
        }

        private void CreateVoronoiDiagram()
        {
            double width = Application.Current.MainWindow.Width;
            double height = Application.Current.MainWindow.Height;

            List<Color> colors = ColorUtils.GenerateRandomColors(MaxPoints);
            List<Point> points = CreateRandomPoints(width, height);

            _voronoi = new Voronoi(points, colors, new Rect(0, 0, width, height));

            foreach (LineSegment lineSegment in _voronoi.VoronoiDiagram())
            {
                if(!lineSegment.P0.HasValue || !lineSegment.P1.HasValue)
                    continue;

                ShapeSegments.Add(new Line
                {
                    X1 = lineSegment.P0.Value.X,
                    X2 = lineSegment.P1.Value.X,

                    Y1 = lineSegment.P0.Value.Y,
                    Y2 = lineSegment.P1.Value.Y,

                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1
                });
            }

            int index = 0;
            foreach (List<Point> point in _voronoi.Regions())
            {
                Polygon polygon = new Polygon
                {
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1,
                    Fill = new SolidColorBrush(colors[index % colors.Count]),
                    Points = new PointCollection(point)
                };
                ShapeSegments.Add(polygon);

                index++;
            }

            foreach (LineSegment lineSegment in _voronoi.DelaunayTriangulation())
            {
                if (!lineSegment.P0.HasValue || !lineSegment.P1.HasValue)
                    continue;

                ShapeSegments.Add(new Line
                {
                    X1 = lineSegment.P0.Value.X,
                    X2 = lineSegment.P1.Value.X,

                    Y1 = lineSegment.P0.Value.Y,
                    Y2 = lineSegment.P1.Value.Y,

                    Stroke = new SolidColorBrush(Colors.LightGray),
                    StrokeThickness = 1
                });
            }

            foreach (Point point in points)
            {
                var ellipse = new Ellipse
                {
                    Stroke = new SolidColorBrush(Colors.Wheat),
                    StrokeThickness = 5
                };

                Canvas.SetLeft(ellipse, point.X - ellipse.StrokeThickness / 2);
                Canvas.SetTop(ellipse, point.Y - ellipse.StrokeThickness / 2);

                ShapeSegments.Add(ellipse);
            }
        }

        private List<Point> CreateRandomPoints(double width, double height)
        {
            List<Point> points = new List<Point>();
            
            for (int i = 0; i < MaxPoints; i++)
            {
                var x = StaticRandom.Instance.Next(0, (int)width);
                var y = StaticRandom.Instance.Next(0, (int)height);

                points.Add(new Point(x, y));
            }

            return points;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}