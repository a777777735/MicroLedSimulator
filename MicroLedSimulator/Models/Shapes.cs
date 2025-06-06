using System.Windows;

namespace MicroLedSimulator.Models
{
    public abstract class DrawingShapeBase
    {
        // Common properties for all shapes can be added here later
        // For now, it serves as a base type for polymorphism
    }

    public class CircleShape : DrawingShapeBase
    {
        public Point Center { get; set; }
        public double Radius { get; set; }

        public CircleShape(Point center, double radius)
        {
            Center = center;
            Radius = radius;
        }
    }

    public class LineShape : DrawingShapeBase
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        public LineShape(Point startPoint, Point endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
    }

    public class CrosshairShape : DrawingShapeBase
    {
        public Point Position { get; set; }
        public double Size { get; set; } // Size could be the length of each arm of the crosshair

        public CrosshairShape(Point position, double size = 10) // Default size
        {
            Position = position;
            Size = size;
        }
    }

    public class PolygonShape : DrawingShapeBase
    {
        public List<Point> Vertices { get; set; }

        public PolygonShape(List<Point> vertices)
        {
            Vertices = vertices ?? new List<Point>();
        }
    }

    public class TextShape : DrawingShapeBase
    {
        public Point Position { get; set; }
        public string Text { get; set; }
        public double FontSize { get; set; }
        public System.Windows.Media.Brush FillBrush { get; set; } // Using System.Windows.Media.Brush

        public TextShape(Point position, string text, double fontSize = 12, System.Windows.Media.Brush? fillBrush = null)
        {
            Position = position;
            Text = text;
            FontSize = fontSize;
            FillBrush = fillBrush ?? System.Windows.Media.Brushes.Black; // Default to Black
        }
    }
}
