using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MicroLedSimulator.Models; // For CameraMode enum

namespace MicroLedSimulator.Controls.CameraFrameControl
{
    public partial class CameraFrameControl : UserControl
    {
        // ViewModel 實例 (在 XAML 中創建或在這裡創建)
        // private CameraFrameControlViewModel ViewModel => DataContext as CameraFrameControlViewModel;

        #region Dependency Properties (用於從外部綁定數據)

        public static readonly DependencyProperty CurrentAppModeProperty =
            DependencyProperty.Register("CurrentAppMode", typeof(CameraMode), typeof(CameraFrameControl),
                new PropertyMetadata(CameraMode.Simulation, OnCurrentAppModeChanged));

        public CameraMode CurrentAppMode
        {
            get { return (CameraMode)GetValue(CurrentAppModeProperty); }
            set { SetValue(CurrentAppModeProperty, value); }
        }

        private static void OnCurrentAppModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CameraFrameControl control && control.DataContext is CameraFrameControlViewModel vm)
            {
                vm.CurrentDisplayMode = (CameraMode)e.NewValue;
            }
        }

        // FilmX
        public static readonly DependencyProperty FilmXProperty =
            DependencyProperty.Register("FilmX", typeof(double), typeof(CameraFrameControl),
                new PropertyMetadata(0.0, OnFilmXChanged));
        public double FilmX { get => (double)GetValue(FilmXProperty); set => SetValue(FilmXProperty, value); }
        private static void OnFilmXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CameraFrameControl control && control.DataContext is CameraFrameControlViewModel vm)
            {
                vm.CurrentBlueFilmPositionX_mm = (double)e.NewValue;
            }
        }

        // FilmY
        public static readonly DependencyProperty FilmYProperty =
            DependencyProperty.Register("FilmY", typeof(double), typeof(CameraFrameControl),
                new PropertyMetadata(0.0, OnFilmYChanged));
        public double FilmY { get => (double)GetValue(FilmYProperty); set => SetValue(FilmYProperty, value); }
        private static void OnFilmYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CameraFrameControl control && control.DataContext is CameraFrameControlViewModel vm)
            {
                vm.CurrentBlueFilmPositionY_mm = (double)e.NewValue;
            }
        }

        // DiesToRender (List<...>)
        public static readonly DependencyProperty DiesToRenderProperty =
            DependencyProperty.Register("DiesToRender", typeof(List<(double, double)>), typeof(CameraFrameControl),
                new PropertyMetadata(null, OnDiesToRenderChanged)); // Default null
        public List<(double, double)> DiesToRender { get => (List<(double, double)>)GetValue(DiesToRenderProperty); set => SetValue(DiesToRenderProperty, value); }
        private static void OnDiesToRenderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CameraFrameControl control && control.DataContext is CameraFrameControlViewModel vm && e.NewValue is List<(double, double)> newList)
            {
                vm.CurrentDiePositionsOnFilm = newList;
            }
        }

        // LogicalDieWidthMm
        public static readonly DependencyProperty LogicalDieWidthMmProperty =
             DependencyProperty.Register("LogicalDieWidthMm", typeof(double), typeof(CameraFrameControl),
                new PropertyMetadata(0.075, OnLogicalDieWidthMmChanged));
        public double LogicalDieWidthMm { get => (double)GetValue(LogicalDieWidthMmProperty); set => SetValue(LogicalDieWidthMmProperty, value); }
        private static void OnLogicalDieWidthMmChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CameraFrameControl control && control.DataContext is CameraFrameControlViewModel vm)
            {
                vm.LogicalDieWidthMm = (double)e.NewValue;
            }
        }

        // LogicalDieHeightMm
        public static readonly DependencyProperty LogicalDieHeightMmProperty =
             DependencyProperty.Register("LogicalDieHeightMm", typeof(double), typeof(CameraFrameControl),
                new PropertyMetadata(0.175, OnLogicalDieHeightMmChanged));
        public double LogicalDieHeightMm { get => (double)GetValue(LogicalDieHeightMmProperty); set => SetValue(LogicalDieHeightMmProperty, value); }
        private static void OnLogicalDieHeightMmChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CameraFrameControl control && control.DataContext is CameraFrameControlViewModel vm)
            {
                vm.LogicalDieHeightMm = (double)e.NewValue;
            }
        }


        #endregion

        private Point _lastMousePosition;
        private bool _isDragging = false;

        public CameraFrameControl()
        {
            InitializeComponent();
            // DataContext is set in XAML to a new instance of CameraFrameControlViewModel
            // 如果需要在 ViewModel 創建後立即進行一些初始化，可以考慮在 Loaded 事件中進行，
            // 或者確保依賴屬性的初始值能觸發 ViewModel 的更新。
            this.Loaded += (s, e) => {
                // 確保在加載時，如果ViewModel已存在且有初始數據，則觸發一次渲染
                if (this.DataContext is CameraFrameControlViewModel vm)
                {
                    // vm.TriggerRenderUpdates(); // ViewModel的屬性setter會觸發
                }
            };
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (DataContext is CameraFrameControlViewModel vm && vm.CurrentDisplayMode == CameraMode.Simulation)
            {
                _lastMousePosition = e.GetPosition(this);
                _isDragging = true;
                Mouse.Capture(this);
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging && DataContext is CameraFrameControlViewModel vm && vm.CurrentDisplayMode == CameraMode.Simulation)
            {
                Point currentMousePosition = e.GetPosition(this);
                Point delta = new Point(
                    currentMousePosition.X - _lastMousePosition.X,
                    currentMousePosition.Y - _lastMousePosition.Y
                );

                // 調用 ViewModel 的命令來處理拖曳
                if (vm.ProcessDragDeltaCommand.CanExecute(delta))
                {
                    vm.ProcessDragDeltaCommand.Execute(delta);
                }

                _lastMousePosition = currentMousePosition;
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isDragging)
            {
                _isDragging = false;
                Mouse.Capture(null);
                e.Handled = true;
            }
        }
    }
}