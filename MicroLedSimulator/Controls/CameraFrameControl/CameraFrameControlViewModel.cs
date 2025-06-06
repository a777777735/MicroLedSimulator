// Controls/CameraFrameControl/CameraFrameControlViewModel.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows; // 需要引用 PresentationCore, PresentationFramework, WindowsBase
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroLedSimulator.Models;
using MicroLedSimulator.Renderers;
// Utils 命名空間根據您的 DrawingUtils.cs 位置決定
// using MicroLedSimulator.Utils; 

namespace MicroLedSimulator.Controls.CameraFrameControl
{
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


        public void TriggerRenderUpdates()
        {
            Debug.WriteLine($"CameraFrameControlViewModel.TriggerRenderUpdates: Mode={CurrentDisplayMode}, MinimapVisible={IsMinimapVisible}, FilmX={CurrentBlueFilmPositionX_mm}");

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
            }
            else
            {
                _primaryRenderer.ClearLayer();
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