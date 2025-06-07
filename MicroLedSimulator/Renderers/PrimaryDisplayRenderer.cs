using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows; // Required for Point
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MicroLedSimulator.Models; // Required for DrawingShapeBase and subclasses
// MicroLedSimulator.Utils might not be needed if all drawing is done via DrawingContext
// using MicroLedSimulator.Utils;

namespace MicroLedSimulator.Renderers
{
    public class PrimaryDisplayRenderer
    {
        private WriteableBitmap? _bitmap;
        public ImageSource? BitmapSource => _bitmap;
        private readonly DrawingVisual _drawingVisual;
        private readonly int _pixelWidth;
        private readonly int _pixelHeight;

        public PrimaryDisplayRenderer(int pixelWidth, int pixelHeight)
        {
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
            _bitmap = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32, null);
            _drawingVisual = new DrawingVisual(); // This is the root visual
        }

        public void UpdateLayer(bool isSimulationMode,
                                double blueFilmPositionX_mm, double blueFilmPositionY_mm,
                                List<(double relX_mm, double relY_mm)> relativeDiePositions,
                                double currentDieWidthMm, double currentDieHeightMm,
                                Color dieColor, Color blueFilmEdgeColor, int blueFilmEdgeThickness,
                                double cameraPixelWidth_const, double cameraPixelHeight_const,
                                double pixelsPerMmX_const, double pixelsPerMmY_const,
                                double blueFilmRadiusMm_const)
        {
            DrawingVisual baseLayerVisual = new DrawingVisual();
            using (DrawingContext dc = baseLayerVisual.RenderOpen())
            {
                // Clear background for this layer (optional, if root visual isn't cleared)
                // dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, _pixelWidth, _pixelHeight));

                if (isSimulationMode)
                {
                    // Draw Blue Film Edge
                    double filmCenterX_inCamera_mm = -blueFilmPositionX_mm;
                    double filmCenterY_inCamera_mm = -blueFilmPositionY_mm;
                    double filmCenterCanvasX_px = (cameraPixelWidth_const / 2.0) + (filmCenterX_inCamera_mm * pixelsPerMmX_const);
                    double filmCenterCanvasY_px = (cameraPixelHeight_const / 2.0) + (filmCenterY_inCamera_mm * pixelsPerMmY_const);
                    double filmRadiusCanvasX_px = blueFilmRadiusMm_const * pixelsPerMmX_const;
                    double filmRadiusCanvasY_px = blueFilmRadiusMm_const * pixelsPerMmY_const;
                    double avgRadius = (filmRadiusCanvasX_px + filmRadiusCanvasY_px) / 2.0;
                    dc.DrawEllipse(null, new Pen(new SolidColorBrush(blueFilmEdgeColor), blueFilmEdgeThickness),
                                   new Point(filmCenterCanvasX_px, filmCenterCanvasY_px), avgRadius, avgRadius);

                    // Draw Dies
                    if (relativeDiePositions.Any())
                    {
                        double dieWidthPx = currentDieWidthMm * pixelsPerMmX_const;
                        double dieHeightPx = currentDieHeightMm * pixelsPerMmY_const;
                        SolidColorBrush dieBrush = new SolidColorBrush(dieColor);
                        dieBrush.Freeze();

                        foreach (var (relX_film_mm, relY_film_mm) in relativeDiePositions)
                        {
                            double dieCenterX_cam_mm = relX_film_mm - blueFilmPositionX_mm;
                            double dieCenterY_cam_mm = relY_film_mm - blueFilmPositionY_mm;
                            double dieCanvasCenterX_px = (cameraPixelWidth_const / 2.0) + (dieCenterX_cam_mm * pixelsPerMmX_const);
                            double dieCanvasCenterY_px = (cameraPixelHeight_const / 2.0) + (dieCenterY_cam_mm * pixelsPerMmY_const);
                            double dieCanvasLeft_px = dieCanvasCenterX_px - (dieWidthPx / 2.0);
                            double dieCanvasTop_px = dieCanvasCenterY_px - (dieHeightPx / 2.0);
                            dc.DrawRectangle(dieBrush, null, new Rect(dieCanvasLeft_px, dieCanvasTop_px, dieWidthPx, dieHeightPx));
                        }
                    }
                }
                // If not simulation mode, this visual remains empty (transparent).
            }
            _drawingVisual.Children.Clear(); // Clear previous layers/shapes
            _drawingVisual.Children.Add(baseLayerVisual);
        }

        public void UpdateShapes(IEnumerable<DrawingShapeBase> shapes)
        {
            // Remove previous shapes visual if it exists
            if (_drawingVisual.Children.Count > 1)
            {
                // Assuming shapes visual is always the second child if it exists
                _drawingVisual.Children.RemoveAt(1);
            }

            if (shapes == null || !shapes.Any())
            {
                return; // No shapes to draw, and previous ones (if any) removed.
            }

            DrawingVisual shapesVisual = new DrawingVisual();
            using (DrawingContext dc = shapesVisual.RenderOpen())
            {
                Pen defaultPen = new Pen(Brushes.Red, 2); // Example pen
                defaultPen.Freeze();

                foreach (var shape in shapes)
                {
                    if (shape is CircleShape circle)
                    {
                        dc.DrawEllipse(null, defaultPen, circle.Center, circle.Radius, circle.Radius);
                    }
                    else if (shape is LineShape line)
                    {
                        dc.DrawLine(defaultPen, line.StartPoint, line.EndPoint);
                    }
                    else if (shape is CrosshairShape crosshair)
                    {
                        double size = crosshair.Size;
                        Point pos = crosshair.Position;
                        dc.DrawLine(defaultPen, new Point(pos.X - size / 2, pos.Y), new Point(pos.X + size / 2, pos.Y));
                        dc.DrawLine(defaultPen, new Point(pos.X, pos.Y - size / 2), new Point(pos.X, pos.Y + size / 2));
                    }
                    else if (shape is PolygonShape polygon)
                    {
                        if (polygon.Vertices != null && polygon.Vertices.Count >= 2)
                        {
                            PathFigure pathFigure = new PathFigure
                            {
                                StartPoint = polygon.Vertices[0],
                                IsClosed = true, // Close the polygon
                                IsFilled = true  // Fill the polygon
                            };

                            if (polygon.Vertices.Count > 1) // Need at least two points for PolyLineSegment
                            {
                                PolyLineSegment polyLineSegment = new PolyLineSegment();
                                for (int i = 1; i < polygon.Vertices.Count; i++)
                                {
                                    polyLineSegment.Points.Add(polygon.Vertices[i]);
                                }
                                pathFigure.Segments.Add(polyLineSegment);
                            }

                            PathGeometry pathGeometry = new PathGeometry();
                            pathGeometry.Figures.Add(pathFigure);
                            pathGeometry.Freeze();

                            // Using defaultPen for outline, and a semi-transparent red for fill for now
                            Brush fillBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                            fillBrush.Freeze();
                            dc.DrawGeometry(fillBrush, defaultPen, pathGeometry);
                        }
                    }
                    else if (shape is TextShape textShape)
                    {
                        // Ensure Typeface is available or use a system default.
                        // For example, new System.Windows.Media.Typeface("Arial")
                        // Using CultureInfo.CurrentCulture for text rendering conventions.
                        FormattedText formattedText = new FormattedText(
                            textShape.Text,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new System.Windows.Media.Typeface("Segoe UI"), // A common WPF typeface
                            textShape.FontSize,
                            textShape.FillBrush, // Use the brush from the shape
                            VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip // Added PixelsPerDip
                        );

                        // Adjust text alignment if necessary, e.g., textShape.Position can be top-left.
                        dc.DrawText(formattedText, textShape.Position);
                    }
                }
            }
            _drawingVisual.Children.Add(shapesVisual); // Add new shapes visual
        }

        public void FinalizeFrame()
        {
            RenderVisualToBitmap();
        }

        private void RenderVisualToBitmap()
        {
            if (_bitmap == null) return;
            _bitmap.Lock();
            try
            {
                // Render the _drawingVisual to the _bitmap.
                // The _drawingVisual contains all elements (base layer + shapes).
                RenderTargetBitmap rtb = new RenderTargetBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(_drawingVisual);
                rtb.Freeze(); // Performance

                // Copy to our WriteableBitmap
                FormatConvertedBitmap fcb = new FormatConvertedBitmap(rtb, PixelFormats.Pbgra32, null, 0);
                _bitmap.WritePixels(new Int32Rect(0, 0, _pixelWidth, _pixelHeight), fcb, _pixelWidth * 4, 0);

                _bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _pixelWidth, _pixelHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }
        }

        public void ClearLayer()
        {
            // This method now effectively clears the visual and then renders that empty visual to the bitmap.
            _drawingVisual.Children.Clear();
            // Optionally, draw a solid background if transparent is not desired for a cleared state
            // DrawingVisual clearVisual = new DrawingVisual();
            // using (DrawingContext dc = clearVisual.RenderOpen())
            // {
            //    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, _pixelWidth, _pixelHeight)); // Example with white
            // }
            // _drawingVisual.Children.Add(clearVisual);
            FinalizeFrame(); // Render the cleared state
        }
    }
}