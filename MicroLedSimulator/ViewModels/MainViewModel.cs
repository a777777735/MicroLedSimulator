// ViewModels/MainViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices; // 雖然 ObservableObject 會處理，但習慣性保留有時無害
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroLedSimulator.Models;
// 不再直接引用 Renderers 或 Utils，這些由 CameraFrameControlViewModel 處理 (如果採用該架構)
// 但如果 MainViewModel 仍需直接控制某些底層，則可能需要
using Microsoft.Win32;


namespace MicroLedSimulator.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // --- 常數 ---
        public const double CameraPixelWidth = 2448;
        public const double CameraPixelHeight = 2048;
        public const double CameraRealWidthMm = 2.82;
        public const double CameraRealHeightMm = 2.36;
        public const double BlueFilmDiameterMm = 152.4;
        public const double MinimapPixelWidth = 200;  // 這些是給 CameraFrameControlViewModel 的參考
        public const double MinimapPixelHeight = 150; // 同上

        // --- 計算屬性 ---
        public double BlueFilmRadiusMm => BlueFilmDiameterMm / 2.0;
        public double PixelsPerMmX => CameraPixelWidth / CameraRealWidthMm;
        public double PixelsPerMmY => CameraPixelHeight / CameraRealHeightMm;

        // --- 使用者輸入和應用程式狀態 ---
        [ObservableProperty]
        private string _dieWidthUmText = "75";
        partial void OnDieWidthUmTextChanged(string? oldValue, string? newValue) => UpdateSimulationParameters();

        [ObservableProperty]
        private string _dieHeightUmText = "175";
        partial void OnDieHeightUmTextChanged(string? oldValue, string? newValue) => UpdateSimulationParameters();

        [ObservableProperty]
        private string _dieSpacingUmText = "5";
        partial void OnDieSpacingUmTextChanged(string? oldValue, string? newValue) => UpdateSimulationParameters();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BlueFilmDisplayX_mm))]
        private double _blueFilmPositionX_mm = 0;
        partial void OnBlueFilmPositionX_mmChanged(double oldValue, double newValue) => UpdateSimulationParameters();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BlueFilmDisplayY_mm))]
        private double _blueFilmPositionY_mm = 0;
        partial void OnBlueFilmPositionY_mmChanged(double oldValue, double newValue) => UpdateSimulationParameters();

        public string BlueFilmDisplayX_mm => _blueFilmPositionX_mm.ToString("F3");
        public string BlueFilmDisplayY_mm => _blueFilmPositionY_mm.ToString("F3");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSimulationMode))] // 當 SelectedMode 改變，這些計算屬性也需要通知更新
        [NotifyPropertyChangedFor(nameof(IsStaticImageMode))]
        [NotifyPropertyChangedFor(nameof(IsRealTimeMode))]
        private CameraMode _selectedMode = CameraMode.Simulation;
        partial void OnSelectedModeChanged(CameraMode oldValue, CameraMode newValue)
        {
            HandleModeChange(); // 處理模式切換的特定邏輯
            UpdateSimulationParameters(); // 更新可能依賴於模式的模擬參數
        }

        // 計算屬性，用於 XAML 中的 Visibility 綁定等
        public bool IsSimulationMode => SelectedMode == CameraMode.Simulation;
        public bool IsStaticImageMode => SelectedMode == CameraMode.StaticImage;
        public bool IsRealTimeMode => SelectedMode == CameraMode.RealTime; // 確保這個存在

        [ObservableProperty]
        private ImageSource? _staticImageSourceForMainView; // 如果 MainWindow 直接顯示靜態圖片

        [ObservableProperty]
        private ImageSource? _realTimeImageSourceForMainView; // 如果 MainWindow 直接顯示即時饋送

        [ObservableProperty]
        private bool _isRealTimeFeedActive = false;
        partial void OnIsRealTimeFeedActiveChanged(bool oldValue, bool newValue)
        {
            // 使用 SelectedMode 屬性進行判斷
            if (newValue && SelectedMode == CameraMode.RealTime) StartRealTimeFeed();
            else StopRealTimeFeed();
        }

        [ObservableProperty]
        private string _errorMessage = "";

        // --- 數據傳遞給 CameraFrameControlViewModel 的屬性 ---
        // 這些屬性會被 CameraFrameControl 的依賴屬性綁定
        [ObservableProperty]
        private CameraMode _simulationModeForControl; // 之前叫 SelectedModeForControl，改為更明確的 SimulationMode
        [ObservableProperty]
        private double _filmXForControl;
        [ObservableProperty]
        private double _filmYForControl;
        [ObservableProperty]
        private List<(double relX_mm, double relY_mm)> _diesForControl = new List<(double, double)>();
        [ObservableProperty]
        private double _dieWidthMmForControl;
        [ObservableProperty]
        private double _dieHeightMmForControl;


        // --- 命令 ---
        public IRelayCommand UpdateDiesUserCommand { get; }
        public IRelayCommand SelectImageCommand { get; }
        public IRelayCommand ToggleRealTimeFeedCommand { get; }
        // FilmDragCommand 現在由 CameraFrameControlViewModel 內部處理，MainViewModel 不再直接擁有它


        // 內部邏輯數據
        private List<(double relX_mm, double relY_mm)> _logicalDiePositionsOnFilmMm = new List<(double, double)>();
        private double _logicalCurrentDieWidthMm, _logicalCurrentDieHeightMm;


        public MainViewModel()
        {
            UpdateDiesUserCommand = new RelayCommand(UpdateSimulationParameters);
            SelectImageCommand = new RelayCommand(ExecuteSelectImage);
            ToggleRealTimeFeedCommand = new RelayCommand(() => IsRealTimeFeedActive = !IsRealTimeFeedActive);
            // FilmDragCommand 不再在這裡初始化

            UpdateSimulationParameters(); // 初始計算一次
        }

        // FilmDrag 由 CameraFrameControlViewModel 處理，然後通過依賴屬性更新 MainViewModel 的藍膜位置
        // 所以 MainViewModel 不需要直接的 FilmDragCommand 了。
        // 如果需要從 CameraFrameControlViewModel 將拖曳結果同步回 MainViewModel，
        // 可以通過事件或者讓 CameraFrameControl 的依賴屬性支持 TwoWay 綁定 (但通常 DP 的回調是 OneWay)。

        private void UpdateSimulationParameters()
        {
            Debug.WriteLine("MainViewModel.UpdateSimulationParameters called");
            ErrorMessage = "";
            double dieWidthUm, dieHeightUm, dieSpacingUm;

            if (!double.TryParse(_dieWidthUmText, NumberStyles.Any, CultureInfo.InvariantCulture, out dieWidthUm) ||
                !double.TryParse(_dieHeightUmText, NumberStyles.Any, CultureInfo.InvariantCulture, out dieHeightUm) ||
                !double.TryParse(_dieSpacingUmText, NumberStyles.Any, CultureInfo.InvariantCulture, out dieSpacingUm))
            {
                ErrorMessage = "輸入無效。請為晶粒參數輸入有效的數值。";
                DiesForControl = new List<(double, double)>(); // 清空或設置為無效狀態
                // 其他 ...ForControl 屬性也可能需要更新
                return;
            }

            if (dieWidthUm <= 0 || dieHeightUm <= 0 || dieSpacingUm < 0)
            {
                ErrorMessage = "晶粒尺寸必須大於零，間距必須為非負值。";
                DiesForControl = new List<(double, double)>();
                return;
            }

            _logicalCurrentDieWidthMm = dieWidthUm / 1000.0;
            _logicalCurrentDieHeightMm = dieHeightUm / 1000.0;
            double spacingMm = dieSpacingUm / 1000.0;

            _logicalDiePositionsOnFilmMm.Clear();
            double generationRegionRadiusMm = BlueFilmRadiusMm * 0.95;
            double yIncrement = _logicalCurrentDieHeightMm + spacingMm;
            double xIncrement = _logicalCurrentDieWidthMm + spacingMm;
            if (yIncrement <= 0) yIncrement = Math.Max(_logicalCurrentDieHeightMm, 0.000001);
            if (xIncrement <= 0) xIncrement = Math.Max(_logicalCurrentDieWidthMm, 0.000001);

            for (double y_rel_film = -generationRegionRadiusMm; y_rel_film < generationRegionRadiusMm; y_rel_film += yIncrement)
            {
                for (double x_rel_film = -generationRegionRadiusMm; x_rel_film < generationRegionRadiusMm; x_rel_film += xIncrement)
                {
                    if (Math.Sqrt(x_rel_film * x_rel_film + y_rel_film * y_rel_film) < (BlueFilmRadiusMm - Math.Max(_logicalCurrentDieWidthMm, _logicalCurrentDieHeightMm) / 2.0))
                    {
                        _logicalDiePositionsOnFilmMm.Add((x_rel_film, y_rel_film));
                    }
                }
            }

            // 更新將傳遞給 CameraFrameControlViewModel 的屬性
            SimulationModeForControl = SelectedMode; // 確保這個屬性被更新
            FilmXForControl = BlueFilmPositionX_mm;
            FilmYForControl = BlueFilmPositionY_mm;
            DiesForControl = new List<(double, double)>(_logicalDiePositionsOnFilmMm);
            DieWidthMmForControl = _logicalCurrentDieWidthMm;
            DieHeightMmForControl = _logicalCurrentDieHeightMm;
        }

        private void HandleModeChange()
        {
            Debug.WriteLine($"MainViewModel.HandleModeChange: New mode is {SelectedMode}");
            StopRealTimeFeed(); // 確保即時饋送被停止
            // UpdateSimulationParameters() 會在 OnSelectedModeChanged 中被調用，
            // 它會更新 SimulationModeForControl 等屬性，
            // CameraFrameControlViewModel 會響應這些屬性的變化來決定是否清除或重新渲染。
        }

        // --- 即時模式相關 ---
        private DispatcherTimer? _realTimeTimer;
        private Random _random = new Random();
        private WriteableBitmap? _realTimeBitmapForMainView;

        private void StartRealTimeFeed()
        {
            // 使用 SelectedMode 屬性進行判斷
            if (SelectedMode != CameraMode.RealTime) return; // 確保只在即時模式下啟動

            if (_realTimeTimer == null)
            {
                _realTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                _realTimeTimer.Tick += RealTimeTimer_Tick;
            }
            if (_realTimeBitmapForMainView == null ||
                _realTimeBitmapForMainView.PixelWidth != (int)CameraPixelWidth ||
                _realTimeBitmapForMainView.PixelHeight != (int)CameraPixelHeight)
            {
                _realTimeBitmapForMainView = new WriteableBitmap((int)CameraPixelWidth, (int)CameraPixelHeight, 96, 96, PixelFormats.Bgr32, null);
            }
            RealTimeImageSourceForMainView = _realTimeBitmapForMainView; // 這是 ObservableProperty
            _realTimeTimer.Start();
            Debug.WriteLine("RealTimeFeed Started");
        }

        private void StopRealTimeFeed()
        {
            _realTimeTimer?.Stop();
            Debug.WriteLine("RealTimeFeed Stopped");
        }

        private void RealTimeTimer_Tick(object? sender, EventArgs e)
        {
            if (_realTimeBitmapForMainView == null || SelectedMode != CameraMode.RealTime || !IsRealTimeFeedActive) return;
            try
            {
                _realTimeBitmapForMainView.Lock();
                IntPtr pBackBuffer = _realTimeBitmapForMainView.BackBuffer;
                int stride = _realTimeBitmapForMainView.BackBufferStride;
                for (int y_rt = 0; y_rt < _realTimeBitmapForMainView.PixelHeight; y_rt++)
                {
                    for (int x_rt = 0; x_rt < _realTimeBitmapForMainView.PixelWidth; x_rt++)
                    {
                        unsafe
                        {
                            byte* pPixel = (byte*)pBackBuffer + y_rt * stride + x_rt * 4;
                            pPixel[0] = (byte)_random.Next(256); pPixel[1] = (byte)_random.Next(256);
                            pPixel[2] = (byte)_random.Next(256); pPixel[3] = 255;
                        }
                    }
                }
                _realTimeBitmapForMainView.AddDirtyRect(new Int32Rect(0, 0, _realTimeBitmapForMainView.PixelWidth, _realTimeBitmapForMainView.PixelHeight));
            }
            finally { _realTimeBitmapForMainView.Unlock(); }
        }

        private void ExecuteSelectImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "圖片檔案 (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|所有檔案 (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    StaticImageSourceForMainView = bitmap; // 這是 ObservableProperty
                }
                catch (Exception ex) { ErrorMessage = $"載入圖片錯誤： {ex.Message}"; } // ErrorMessage 是 ObservableProperty
            }
        }
    }
}