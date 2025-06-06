using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroLedSimulator.Controls.CameraFrameControl;
using MicroLedSimulator.Models; // For MeasurementToolType
using System.Windows; // For Point

namespace MicroLedSimulator.Tests
{
    [TestClass]
    public class CameraFrameControlViewModelTests
    {
        private const double Delta = 0.001; // For floating point comparisons

        [TestMethod]
        public void TestPixelDistance()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.SetActiveTool(MeasurementToolType.PixelDistance); // Use the new helper

            // AddMeasurementPointCommand takes a Point?, so we pass new Point(x,y)
            viewModel.AddMeasurementPointCommand.Execute(new Point(0, 0));
            viewModel.AddMeasurementPointCommand.Execute(new Point(3, 4));

            Assert.IsTrue(viewModel.MeasurementResultText.Contains("Distance: 5.00 pixels"), $"Unexpected result: {viewModel.MeasurementResultText}");
        }

        [TestMethod]
        public void TestAngleCalculation_RightAngle()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.SetActiveTool(MeasurementToolType.Angle);

            viewModel.AddMeasurementPointCommand.Execute(new Point(0, 0)); // P1
            viewModel.AddMeasurementPointCommand.Execute(new Point(1, 0)); // Vertex P2
            viewModel.AddMeasurementPointCommand.Execute(new Point(1, 1)); // P3

            // Expected angle is 90 degrees.
            // The string result is "Angle: 90.00 degrees"
            // We need to parse this or make CalculateAngle public for direct testing.
            // For now, we test the string output.
            Assert.IsTrue(viewModel.MeasurementResultText.Contains("Angle: 90.00 degrees"), $"Unexpected result: {viewModel.MeasurementResultText}");
        }

        [TestMethod]
        public void TestAngleCalculation_StraightAngle()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.SetActiveTool(MeasurementToolType.Angle);

            viewModel.AddMeasurementPointCommand.Execute(new Point(0, 0)); // P1
            viewModel.AddMeasurementPointCommand.Execute(new Point(1, 0)); // Vertex P2
            viewModel.AddMeasurementPointCommand.Execute(new Point(2, 0)); // P3

            Assert.IsTrue(viewModel.MeasurementResultText.Contains("Angle: 180.00 degrees"), $"Unexpected result: {viewModel.MeasurementResultText}");
        }

        [TestMethod]
        public void TestAngleCalculation_45Degrees()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.SetActiveTool(MeasurementToolType.Angle);

            viewModel.AddMeasurementPointCommand.Execute(new Point(2, 0)); // P1 (on X axis)
            viewModel.AddMeasurementPointCommand.Execute(new Point(0, 0)); // Vertex P2 (origin)
            viewModel.AddMeasurementPointCommand.Execute(new Point(1, 1)); // P3 (y=x line)

            Assert.IsTrue(viewModel.MeasurementResultText.Contains("Angle: 45.00 degrees"), $"Unexpected result: {viewModel.MeasurementResultText}");
        }


        [TestMethod]
        public void TestAreaCalculation_Square()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.SetActiveTool(MeasurementToolType.Area);

            viewModel.AddMeasurementPointCommand.Execute(new Point(0, 0));
            viewModel.AddMeasurementPointCommand.Execute(new Point(10, 0));
            viewModel.AddMeasurementPointCommand.Execute(new Point(10, 5));
            viewModel.AddMeasurementPointCommand.Execute(new Point(0, 5));

            viewModel.CompleteAreaMeasurementCommand.Execute(null);

            Assert.IsTrue(viewModel.MeasurementResultText.Contains("Area: 50.00 square pixels"), $"Unexpected result: {viewModel.MeasurementResultText}");
        }

        [TestMethod]
        public void TestAreaCalculation_Triangle()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.SetActiveTool(MeasurementToolType.Area);

            viewModel.AddMeasurementPointCommand.Execute(new Point(0, 0));
            viewModel.AddMeasurementPointCommand.Execute(new Point(10, 0));
            viewModel.AddMeasurementPointCommand.Execute(new Point(5, 5));

            viewModel.CompleteAreaMeasurementCommand.Execute(null);

            // Area = 0.5 * base * height = 0.5 * 10 * 5 = 25
            Assert.IsTrue(viewModel.MeasurementResultText.Contains("Area: 25.00 square pixels"), $"Unexpected result: {viewModel.MeasurementResultText}");
        }

        [TestMethod]
        public void TestMeasurementToolActivates_DisablesDrawingTool()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.DrawCircleCommand.Execute(null); // Setup: Activate a drawing tool
            Assert.AreEqual(DrawingToolType.Circle, viewModel.ActiveDrawingTool, "Initial drawing tool setup failed.");


            viewModel.MeasurePixelDistanceCommand.Execute(null); // Action: Activate a measurement tool

            Assert.AreEqual(MeasurementToolType.PixelDistance, viewModel.ActiveMeasurementTool, "Measurement tool was not activated.");
            Assert.AreEqual(DrawingToolType.None, viewModel.ActiveDrawingTool, "Drawing tool was not disabled.");
        }

        [TestMethod]
        public void TestDrawingToolActivates_DisablesMeasurementTool()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.MeasurePixelDistanceCommand.Execute(null); // Setup: Activate a measurement tool
            Assert.AreEqual(MeasurementToolType.PixelDistance, viewModel.ActiveMeasurementTool, "Initial measurement tool setup failed.");

            viewModel.DrawCircleCommand.Execute(null); // Action: Activate a drawing tool

            Assert.AreEqual(DrawingToolType.Circle, viewModel.ActiveDrawingTool, "Drawing tool was not activated.");
            Assert.AreEqual(MeasurementToolType.None, viewModel.ActiveMeasurementTool, "Measurement tool was not disabled.");
        }

        [TestMethod]
        public void TestDrawCircle_AddsShapeToList()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.DrawCircleCommand.Execute(null); // Activates Circle tool

            Point center = new Point(10, 10);
            Point radiusPoint = new Point(10, 15); // Radius of 5

            viewModel.AddDrawingPointCommand.Execute(center);
            viewModel.AddDrawingPointCommand.Execute(radiusPoint);

            Assert.AreEqual(1, viewModel.DrawnShapes.Count, "Shape was not added to the list.");
            Assert.IsInstanceOfType(viewModel.DrawnShapes[0], typeof(CircleShape), "Shape is not a CircleShape.");

            var circle = viewModel.DrawnShapes[0] as CircleShape;
            Assert.IsNotNull(circle, "Circle shape is null after cast.");
            Assert.AreEqual(center, circle.Center, "Circle center is incorrect.");
            Assert.AreEqual(5, circle.Radius, Delta, "Circle radius is incorrect.");
        }

        [TestMethod]
        public void TestDrawLine_AddsShapeToList()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.DrawLineCommand.Execute(null);

            Point p1 = new Point(5, 5);
            Point p2 = new Point(10, 10);

            viewModel.AddDrawingPointCommand.Execute(p1);
            viewModel.AddDrawingPointCommand.Execute(p2);

            Assert.AreEqual(1, viewModel.DrawnShapes.Count, "Shape was not added.");
            Assert.IsInstanceOfType(viewModel.DrawnShapes[0], typeof(LineShape), "Shape is not a LineShape.");

            var line = viewModel.DrawnShapes[0] as LineShape;
            Assert.IsNotNull(line, "Line shape is null.");
            Assert.AreEqual(p1, line.StartPoint, "Line StartPoint is incorrect.");
            Assert.AreEqual(p2, line.EndPoint, "Line EndPoint is incorrect.");
        }

        [TestMethod]
        public void TestDrawCrosshair_AddsShapeToList()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.DrawCrosshairCommand.Execute(null);

            Point p = new Point(20, 25);
            viewModel.AddDrawingPointCommand.Execute(p);

            Assert.AreEqual(1, viewModel.DrawnShapes.Count, "Shape was not added.");
            Assert.IsInstanceOfType(viewModel.DrawnShapes[0], typeof(CrosshairShape), "Shape is not a CrosshairShape.");

            var crosshair = viewModel.DrawnShapes[0] as CrosshairShape;
            Assert.IsNotNull(crosshair, "Crosshair shape is null.");
            Assert.AreEqual(p, crosshair.Position, "Crosshair Position is incorrect.");
            // Default size is 10, can be asserted if needed: Assert.AreEqual(10, crosshair.Size);
        }

        [TestMethod]
        public void TestDrawPolygon_AddsShapeToList()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.DrawPolygonCommand.Execute(null);

            Point p1 = new Point(0, 0);
            Point p2 = new Point(10, 0);
            Point p3 = new Point(5, 5);

            viewModel.AddDrawingPointCommand.Execute(p1);
            viewModel.AddDrawingPointCommand.Execute(p2);
            viewModel.AddDrawingPointCommand.Execute(p3);
            viewModel.CompletePolygonCommand.Execute(null);

            Assert.AreEqual(1, viewModel.DrawnShapes.Count, "Polygon was not added.");
            Assert.IsInstanceOfType(viewModel.DrawnShapes[0], typeof(PolygonShape), "Shape is not a PolygonShape.");

            var polygon = viewModel.DrawnShapes[0] as PolygonShape;
            Assert.IsNotNull(polygon, "Polygon shape is null.");
            Assert.AreEqual(3, polygon.Vertices.Count, "Polygon vertex count is incorrect.");
            Assert.AreEqual(p1, polygon.Vertices[0]);
            Assert.AreEqual(p2, polygon.Vertices[1]);
            Assert.AreEqual(p3, polygon.Vertices[2]);
        }

        [TestMethod]
        public void TestDrawText_AddsShapeToList()
        {
            var viewModel = new CameraFrameControlViewModel();
            viewModel.DrawTextCommand.Execute(null);

            Point p = new Point(50, 60);
            viewModel.AddDrawingPointCommand.Execute(p);

            Assert.AreEqual(1, viewModel.DrawnShapes.Count, "Shape was not added.");
            Assert.IsInstanceOfType(viewModel.DrawnShapes[0], typeof(TextShape), "Shape is not a TextShape.");

            var textShape = viewModel.DrawnShapes[0] as TextShape;
            Assert.IsNotNull(textShape, "Text shape is null.");
            Assert.AreEqual(p, textShape.Position, "Text Position is incorrect.");
            Assert.AreEqual("Sample Text", textShape.Text, "Default text is incorrect.");
            // Assert.AreEqual(16, textShape.FontSize); // Default font size check
        }
    }
}
