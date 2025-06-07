// Controls/CameraFrameControl/CameraFrameControlViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Required for ObservableCollection
using System.Diagnostics;
using System.Windows; // 需要引用 PresentationCore, PresentationFramework, WindowsBase
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroLedSimulator.Models; // Assuming Shapes.cs is in MicroLedSimulator.Models
using MicroLedSimulator.Renderers;
// Utils 命名空間根據您的 DrawingUtils.cs 位置決定
// using MicroLedSimulator.Utils; 

namespace MicroLedSimulator.Controls.CameraFrameControl
{
    public enum DrawingToolType { None, Circle, Line, Crosshair, Polygon, Text }
    public enum MeasurementToolType { None, PixelDistance, Angle, Area }

    public partial class CameraFrameControlViewModel : ObservableObject
    {
        private readonly PrimaryDisplayRenderer _primaryRenderer;
        public ImageSource? DieLayerSource => _primaryRenderer.BitmapSource;

        private readonly MinimapDisplayRenderer _minimapRenderer;
        public ImageSource? MinimapSource => _minimapRenderer.BitmapSource;

        [ObservableProperty]
        private bool _isMinimapVisible = false;
        partial void OnIsMinimapVisibleChanged(bool value) => TriggerRenderUpdates();

        public IRelayCommand ToggleMinimapVisibilityCommand { get; }

        // --- 從 CameraFrameControl 依賴屬性接收的數據 ---
        // 這些屬性由依賴屬性的 PropertyChangedCallback 更新
        [ObservableProperty]
        private CameraMode _currentDisplayMode = CameraMode.Simulation; // 注意屬性名，避免與 UserControl 的 Mode 衝突
        partial void OnCurrentDisplayModeChanged(CameraMode value) => TriggerRenderUpdates();

        [ObservableProperty]
        private double _currentBlueFilmPositionX_mm = 0;
        partial void OnCurrentBlueFilmPositionX_mmChanged(double value) => TriggerRenderUpdates();

        [ObservableProperty]
        private double _currentBlueFilmPositionY_mm = 0;
        partial void OnCurrentBlueFilmPositionY_mmChanged(double value) => TriggerRenderUpdates();

        [ObservableProperty]
        private List<(double relX_mm, double relY_mm)> _currentDiePositionsOnFilm = new List<(double, double)>();
        partial void OnCurrentDiePositionsOnFilmChanged(List<(double relX_mm, double relY_mm)> value) => TriggerRenderUpdates();

        [ObservableProperty]
        private double _logicalDieWidthMm = 0.075;
        partial void OnLogicalDieWidthMmChanged(double value) => TriggerRenderUpdates();

        [ObservableProperty]
        private double _logicalDieHeightMm = 0.175;
        partial void OnLogicalDieHeightMmChanged(double value) => TriggerRenderUpdates();


        // --- 繪圖參數 (這些可以保持為常量或從外部傳入) ---
        public Color DieFillColor { get; } = ((SolidColorBrush)Brushes.DodgerBlue).Color;
        public Color MainViewBlueFilmEdgeColor { get; } = Colors.LightSkyBlue;
        public int MainViewBlueFilmEdgeThickness { get; } = 2;
        public Color MinimapBlueFilmFillColor { get; } = Color.FromArgb(200, 40, 40, 60);
        public Color MinimapBlueFilmEdgeColor { get; } = Color.FromArgb(255, 70, 70, 90);
        public Color MinimapViewRectColor { get; } = Colors.LimeGreen;
        public int MinimapViewRectThickness { get; } = 1;
        public int MinimapBlueFilmEdgeThickness { get; } = 1;

        // 命令給滑鼠拖曳（由 CameraFrameControl.xaml.cs 調用）
        public IRelayCommand<Point> ProcessDragDeltaCommand { get; }

        // Drawing Commands
        public IRelayCommand DrawCircleCommand { get; }
        public IRelayCommand DrawLineCommand { get; }
        public IRelayCommand DrawPolygonCommand { get; }
        public IRelayCommand DrawTextCommand { get; }
        public IRelayCommand DrawCrosshairCommand { get; }

        [ObservableProperty]
        private DrawingToolType _activeDrawingTool = DrawingToolType.None;

        public ObservableCollection<DrawingShapeBase> DrawnShapes { get; } = new ObservableCollection<DrawingShapeBase>();
        public ObservableCollection<Point> CurrentDrawingPoints { get; } = new ObservableCollection<Point>();

        public IRelayCommand<Point> AddDrawingPointCommand { get; }
        public IRelayCommand CompletePolygonCommand { get; }

        // Measurement Commands
        public IRelayCommand MeasurePixelDistanceCommand { get; }
        public IRelayCommand MeasureAngleCommand { get; }
        public IRelayCommand MeasureAreaCommand { get; }
        public IRelayCommand<Point> AddMeasurementPointCommand { get; }
        public IRelayCommand CompleteAreaMeasurementCommand { get; }

        [ObservableProperty]
        private MeasurementToolType _activeMeasurementTool = MeasurementToolType.None;

        public ObservableCollection<Point> CurrentMeasurementPoints { get; } = new ObservableCollection<Point>();

        [ObservableProperty]
        private string _measurementResultText = string.Empty;


        public CameraFrameControlViewModel()
        {
            // 使用 ViewModels.MainViewModel 中的靜態常數來獲取尺寸
            _primaryRenderer = new PrimaryDisplayRenderer((int)ViewModels.MainViewModel.CameraPixelWidth, (int)ViewModels.MainViewModel.CameraPixelHeight);
            _minimapRenderer = new MinimapDisplayRenderer((int)ViewModels.MainViewModel.MinimapPixelWidth, (int)ViewModels.MainViewModel.MinimapPixelHeight);

            // 確保初始 ImageSource 被 UI 獲取
            OnPropertyChanged(nameof(DieLayerSource));
            OnPropertyChanged(nameof(MinimapSource));

            ToggleMinimapVisibilityCommand = new RelayCommand(() => IsMinimapVisible = !IsMinimapVisible);
            ProcessDragDeltaCommand = new RelayCommand<Point>(ProcessDragDelta);

            // Initialize Drawing Commands
            DrawCircleCommand = new RelayCommand(ExecuteDrawCircle);
            DrawLineCommand = new RelayCommand(ExecuteDrawLine);
            DrawPolygonCommand = new RelayCommand(ExecuteDrawPolygon);
            DrawTextCommand = new RelayCommand(ExecuteDrawText);
            DrawCrosshairCommand = new RelayCommand(ExecuteDrawCrosshair);

            AddDrawingPointCommand = new RelayCommand<Point>(AddDrawingPoint);
            CompletePolygonCommand = new RelayCommand(ExecuteCompletePolygon, CanExecuteCompletePolygon);

            // Initialize Measurement Commands
            MeasurePixelDistanceCommand = new RelayCommand(ExecuteMeasurePixelDistance);
            MeasureAngleCommand = new RelayCommand(ExecuteMeasureAngle);
            MeasureAreaCommand = new RelayCommand(ExecuteMeasureArea);
            AddMeasurementPointCommand = new RelayCommand<Point>(AddMeasurementPoint);
            CompleteAreaMeasurementCommand = new RelayCommand(ExecuteCompleteAreaMeasurement, CanExecuteCompleteAreaMeasurement);


            // 初始渲染一次 (如果希望在沒有數據傳入時也有默認顯示)
            // TriggerRenderUpdates(); // 或者等待外部數據傳入後再首次渲染
        }

        private void ProcessDragDelta(Point deltaPx)
        {
            // 更新內部的藍膜位置
            // 注意：這裡的 PixelsPerMmX/Y 也是依賴於 MainViewModel 的常數
            // 更好的做法是將這些轉換因子也作為參數或配置傳入
            CurrentBlueFilmPositionX_mm -= deltaPx.X / (ViewModels.MainViewModel.CameraPixelWidth / ViewModels.MainViewModel.CameraRealWidthMm);
            CurrentBlueFilmPositionY_mm -= deltaPx.Y / (ViewModels.MainViewModel.CameraPixelHeight / ViewModels.MainViewModel.CameraRealHeightMm);
            // CurrentBlueFilmPositionX_mm 和 CurrentBlueFilmPositionY_mm 的 setter 會調用 TriggerRenderUpdates
        }

        private void SetActiveTool(DrawingToolType toolType)
        {
            ActiveDrawingTool = toolType;
            ActiveMeasurementTool = MeasurementToolType.None; // Mutually exclusive
            CurrentDrawingPoints.Clear();
            CurrentMeasurementPoints.Clear(); // Clear measurement points as well
            MeasurementResultText = string.Empty; // Clear measurement results
            CompletePolygonCommand.NotifyCanExecuteChanged();
            CompleteAreaMeasurementCommand.NotifyCanExecuteChanged();
            Debug.WriteLine($"ActiveDrawingTool set to {ActiveDrawingTool}, ActiveMeasurementTool set to {ActiveMeasurementTool}");
        }

        private void SetActiveTool(MeasurementToolType toolType)
        {
            ActiveMeasurementTool = toolType;
            ActiveDrawingTool = DrawingToolType.None; // Mutually exclusive
            CurrentDrawingPoints.Clear(); // Clear drawing points
            CurrentMeasurementPoints.Clear();
            MeasurementResultText = string.Empty;
            CompletePolygonCommand.NotifyCanExecuteChanged();
            CompleteAreaMeasurementCommand.NotifyCanExecuteChanged();
            Debug.WriteLine($"ActiveDrawingTool set to {ActiveDrawingTool}, ActiveMeasurementTool set to {ActiveMeasurementTool}");
        }


        private void ExecuteDrawCircle() => SetActiveTool(DrawingToolType.Circle);
        private void ExecuteDrawLine() => SetActiveTool(DrawingToolType.Line);
        private void ExecuteDrawPolygon() => SetActiveTool(DrawingToolType.Polygon);
        private void ExecuteDrawText() => SetActiveTool(DrawingToolType.Text);
        private void ExecuteDrawCrosshair() => SetActiveTool(DrawingToolType.Crosshair);


        private void ExecuteMeasurePixelDistance() => SetActiveTool(MeasurementToolType.PixelDistance);
        private void ExecuteMeasureAngle() => SetActiveTool(MeasurementToolType.Angle);
        private void ExecuteMeasureArea() => SetActiveTool(MeasurementToolType.Area);


        private void AddDrawingPoint(Point? point)
        {
            if (point == null || ActiveDrawingTool == DrawingToolType.None) return;

            CurrentDrawingPoints.Add(point.Value);
            Debug.WriteLine($"Drawing Point added: {point.Value}. Current points: {CurrentDrawingPoints.Count}");

            switch (ActiveDrawingTool)
            {
                case DrawingToolType.Circle:
                    if (CurrentDrawingPoints.Count == 2)
                    {
                        var center = CurrentDrawingPoints[0];
                        var radiusPoint = CurrentDrawingPoints[1];
                        var radius = Math.Sqrt(Math.Pow(radiusPoint.X - center.X, 2) + Math.Pow(radiusPoint.Y - center.Y, 2));
                        DrawnShapes.Add(new CircleShape(center, radius));
                        CurrentDrawingPoints.Clear();
                        // ActiveDrawingTool = DrawingToolType.None; // Optionally reset tool
                        Debug.WriteLine($"Circle drawn: Center={center}, Radius={radius}");
                        TriggerRenderUpdates(); // Update display
                    }
                    break;
                case DrawingToolType.Line:
                    if (CurrentDrawingPoints.Count == 2)
                    {
                        DrawnShapes.Add(new LineShape(CurrentDrawingPoints[0], CurrentDrawingPoints[1]));
                        CurrentDrawingPoints.Clear();
                        // ActiveDrawingTool = DrawingToolType.None; // Optionally reset tool
                        Debug.WriteLine($"Line drawn: Start={DrawnShapes.LastOrDefault()?.GetType().GetProperty("StartPoint")?.GetValue(DrawnShapes.LastOrDefault())}, End={DrawnShapes.LastOrDefault()?.GetType().GetProperty("EndPoint")?.GetValue(DrawnShapes.LastOrDefault())}");
                        TriggerRenderUpdates(); // Update display
                    }
                    break;
                case DrawingToolType.Crosshair:
                    if (CurrentDrawingPoints.Count == 1)
                    {
                        DrawnShapes.Add(new CrosshairShape(CurrentDrawingPoints[0])); // Using default size
                        CurrentDrawingPoints.Clear();
                        // ActiveDrawingTool = DrawingToolType.None; // Optionally reset tool
                        Debug.WriteLine($"Crosshair drawn: Position={DrawnShapes.LastOrDefault()?.GetType().GetProperty("Position")?.GetValue(DrawnShapes.LastOrDefault())}");
                        TriggerRenderUpdates(); // Update display
                    }
                    break;
                case DrawingToolType.Polygon:
                    // Points are added, actual polygon creation happens on CompletePolygonCommand
                    Debug.WriteLine($"Polygon point added: {point.Value}. Total points: {CurrentDrawingPoints.Count}");
                    // Update CanExecute for CompletePolygonCommand after adding a point
                    CompletePolygonCommand.NotifyCanExecuteChanged();
                    // Optionally, could draw a temporary polygon as points are added
                    // TriggerRenderUpdates(); // If wanting to show points as they are added or a temporary polygon
                    break;
                case DrawingToolType.Text:
                    if (CurrentDrawingPoints.Count == 1)
                    {
                        // For simplicity, using predefined text and font size
                        string predefinedText = "Sample Text";
                        double predefinedFontSize = 16;
                        // You could add logic here to open a dialog for text input
                        DrawnShapes.Add(new TextShape(CurrentDrawingPoints[0], predefinedText, predefinedFontSize));
                        CurrentDrawingPoints.Clear();
                        // ActiveDrawingTool = DrawingToolType.None; // Optionally reset tool
                        Debug.WriteLine($"Text shape added at {DrawnShapes.LastOrDefault()?.GetType().GetProperty("Position")?.GetValue(DrawnShapes.LastOrDefault())} with text '{predefinedText}'");
                        TriggerRenderUpdates();
                    }
                    break;
            }
        }

        private void AddMeasurementPoint(Point? point)
        {
            if (point == null || ActiveMeasurementTool == MeasurementToolType.None) return;

            CurrentMeasurementPoints.Add(point.Value);
            Debug.WriteLine($"Measurement Point added: {point.Value}. Current points: {CurrentMeasurementPoints.Count}");

            switch (ActiveMeasurementTool)
            {
                case MeasurementToolType.PixelDistance:
                    if (CurrentMeasurementPoints.Count == 2)
                    {
                        Point p1 = CurrentMeasurementPoints[0];
                        Point p2 = CurrentMeasurementPoints[1];
                        double distance = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                        MeasurementResultText = $"Distance: {distance:F2} pixels";
                        CurrentMeasurementPoints.Clear();
                        // ActiveMeasurementTool = MeasurementToolType.None; // Optionally reset
                        Debug.WriteLine(MeasurementResultText);
                        // TriggerRenderUpdates(); // If visual feedback for points is drawn
                    }
                    break;
                case MeasurementToolType.Angle:
                    if (CurrentMeasurementPoints.Count == 3)
                    {
                        Point p1 = CurrentMeasurementPoints[0]; // First arm point
                        Point p2 = CurrentMeasurementPoints[1]; // Vertex
                        Point p3 = CurrentMeasurementPoints[2]; // Second arm point

                        double angle = CalculateAngle(p1, p2, p3);
                        MeasurementResultText = $"Angle: {angle:F2} degrees";
                        CurrentMeasurementPoints.Clear();
                        // ActiveMeasurementTool = MeasurementToolType.None; // Optionally reset
                        Debug.WriteLine(MeasurementResultText);
                        // TriggerRenderUpdates();
                    }
                    break;
                case MeasurementToolType.Area:
                    // Points are added, area calculation happens on CompleteAreaMeasurementCommand
                    Debug.WriteLine($"Area point added: {point.Value}. Total points: {CurrentMeasurementPoints.Count}");
                    CompleteAreaMeasurementCommand.NotifyCanExecuteChanged();
                    // TriggerRenderUpdates(); // If visual feedback for points is drawn
                    break;
            }
        }

        private double CalculateAngle(Point p1, Point p2, Point p3)
        {
            // Vector p2p1
            Vector v1 = new Vector(p1.X - p2.X, p1.Y - p2.Y);
            // Vector p2p3
            Vector v2 = new Vector(p3.X - p2.X, p3.Y - p2.Y);

            double dotProduct = Vector.Multiply(v1, v2);
            double determinant = v1.X * v2.Y - v1.Y * v2.X; // For signed angle, not strictly needed here for magnitude
            double angleRad = Math.Atan2(determinant, dotProduct); // More robust
            // Or use Acos for unsigned angle:
            // double angleRad = Math.Acos(dotProduct / (v1.Length * v2.Length));

            double angleDeg = angleRad * (180.0 / Math.PI);
            return Math.Abs(angleDeg); // Typically, angle is positive. Or return as is if direction matters.
        }


        private void ExecuteCompletePolygon()
        {
            if (CanExecuteCompletePolygon())
            {
                DrawnShapes.Add(new PolygonShape(new List<Point>(CurrentDrawingPoints)));
                CurrentDrawingPoints.Clear();
                // SetActiveTool(DrawingToolType.None); // Or keep Polygon active
                Debug.WriteLine($"Polygon completed with {DrawnShapes.LastOrDefault()?.GetType().GetProperty("Vertices")?.GetValue(DrawnShapes.LastOrDefault())?.GetType().GetProperty("Count").GetValue(DrawnShapes.LastOrDefault()?.GetType().GetProperty("Vertices")?.GetValue(DrawnShapes.LastOrDefault()))} vertices.");
                TriggerRenderUpdates();
            }
            CompletePolygonCommand.NotifyCanExecuteChanged();
        }

        private bool CanExecuteCompletePolygon()
        {
            return ActiveDrawingTool == DrawingToolType.Polygon && CurrentDrawingPoints.Count >= 3;
        }

        private void ExecuteCompleteAreaMeasurement()
        {
            if (CanExecuteCompleteAreaMeasurement())
            {
                double area = CalculatePolygonArea(CurrentMeasurementPoints);
                MeasurementResultText = $"Area: {area:F2} square pixels";
                CurrentMeasurementPoints.Clear();
                // SetActiveTool(MeasurementToolType.None); // Or keep Area active
                Debug.WriteLine(MeasurementResultText);
                // TriggerRenderUpdates(); // If visual feedback for points is drawn and needs clearing
            }
            CompleteAreaMeasurementCommand.NotifyCanExecuteChanged();
        }

        private bool CanExecuteCompleteAreaMeasurement()
        {
            return ActiveMeasurementTool == MeasurementToolType.Area && CurrentMeasurementPoints.Count >= 3;
        }

        private double CalculatePolygonArea(IList<Point> vertices)
        {
            if (vertices.Count < 3) return 0;
            double area = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                Point p1 = vertices[i];
                Point p2 = vertices[(i + 1) % vertices.Count]; // Wrap around for the last vertex
                area += (p1.X * p2.Y) - (p1.Y * p2.X);
            }
            return Math.Abs(area) / 2.0;
        }


        public void TriggerRenderUpdates()
        {
            Debug.WriteLine($"CameraFrameControlViewModel.TriggerRenderUpdates: Mode={CurrentDisplayMode}, MinimapVisible={IsMinimapVisible}, FilmX={CurrentBlueFilmPositionX_mm}, Shapes={DrawnShapes.Count}");

            bool isSim = CurrentDisplayMode == CameraMode.Simulation;

            if (isSim)
            {
                _primaryRenderer.UpdateLayer(
                    true, CurrentBlueFilmPositionX_mm, CurrentBlueFilmPositionY_mm,
                    CurrentDiePositionsOnFilm, LogicalDieWidthMm, LogicalDieHeightMm,
                    DieFillColor, MainViewBlueFilmEdgeColor, MainViewBlueFilmEdgeThickness,
                    ViewModels.MainViewModel.CameraPixelWidth, ViewModels.MainViewModel.CameraPixelHeight,
                    ViewModels.MainViewModel.CameraPixelWidth / ViewModels.MainViewModel.CameraRealWidthMm,
                    ViewModels.MainViewModel.CameraPixelHeight / ViewModels.MainViewModel.CameraRealHeightMm,
                    ViewModels.MainViewModel.BlueFilmDiameterMm / 2.0);
                _primaryRenderer.UpdateShapes(DrawnShapes);
                _primaryRenderer.FinalizeFrame(); // New call to render the combined visual to bitmap
            }
            else
            {
                _primaryRenderer.ClearLayer(); // ClearLayer should also internally call FinalizeFrame or equivalent
                                               // Or ensure UpdateShapes([]) + FinalizeFrame() is called.
                                               // For now, assuming ClearLayer also finalizes the cleared state to bitmap.
                                               // If ClearLayer just clears the visual, we might need _primaryRenderer.FinalizeFrame() here too.
                                               // Let's assume ClearLayer handles its own finalization for simplicity for now.
            }
            OnPropertyChanged(nameof(DieLayerSource)); // 即使引用不變，也通知內容可能已更新

            if (IsMinimapVisible && isSim)
            {
                _minimapRenderer.UpdateLayer(
                    true, true,
                    CurrentBlueFilmPositionX_mm, CurrentBlueFilmPositionY_mm,
                    MinimapBlueFilmFillColor, MinimapBlueFilmEdgeColor, MinimapBlueFilmEdgeThickness,
                    MinimapViewRectColor, MinimapViewRectThickness,
                    ViewModels.MainViewModel.BlueFilmDiameterMm, ViewModels.MainViewModel.BlueFilmDiameterMm / 2.0,
                    ViewModels.MainViewModel.CameraRealWidthMm, ViewModels.MainViewModel.CameraRealHeightMm);
            }
            else
            {
                _minimapRenderer.ClearLayer();
            }
            OnPropertyChanged(nameof(MinimapSource));
        }
    }
}