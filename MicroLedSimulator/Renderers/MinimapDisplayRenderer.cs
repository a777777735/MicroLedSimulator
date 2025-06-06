using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MicroLedSimulator.Utils; // 確保 using 正確

namespace MicroLedSimulator.Renderers
{
    public class MinimapDisplayRenderer
    {
        private WriteableBitmap? _bitmap;
        public ImageSource? BitmapSource => _bitmap; // ImageSource 綁定到這個

        private readonly int _pixelWidth;
        private readonly int _pixelHeight;

        // Renderers/MinimapDisplayRenderer.cs
        public MinimapDisplayRenderer(int pixelWidth, int pixelHeight)
        {
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
            _bitmap = new WriteableBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Pbgra32, null);
            Debug.WriteLine($"MinimapDisplayRenderer: Bitmap created {_pixelWidth}x{_pixelHeight}.");

            // 在構造時立即用純色填充，以排除 UpdateLayer 的問題
            if (_bitmap != null)
            {
                _bitmap.Lock();
                try
                {
                    DrawingUtils.ClearBitmap(_bitmap); // 先清除
                    Color initialColor = Colors.Fuchsia; // 用一個非常刺眼的顏色
                    DrawingUtils.FillRectangleOnBitmap(_bitmap, 0, 0, _pixelWidth, _pixelHeight, initialColor);
                    _bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _pixelWidth, _pixelHeight));
                    Debug.WriteLine($"MinimapDisplayRenderer: Filled initial bitmap with {initialColor}");
                }
                finally
                {
                    _bitmap.Unlock();
                }
            }
            // ClearLayer(); // 暫時不在這裡清除，以便觀察初始填充
        }

        // UpdateLayer 和 ClearLayer 暫時可以保持原樣，或者也用簡單填充/清除

        public void UpdateLayer(bool isVisible, bool isSimulationMode,
                                // 即使簡化繪製，也保留參數以匹配 ViewModel 調用
                                double blueFilmPositionX_mm, double blueFilmPositionY_mm,
                                Color filmFillColor, Color filmEdgeColor, int filmEdgeThickness,
                                Color viewRectColor, int viewRectThickness,
                                double blueFilmDiameterMm_const, double blueFilmRadiusMm_const,
                                double cameraRealWidthMm_const, double cameraRealHeightMm_const)
        {
            Debug.WriteLine($"MinimapDisplayRenderer.UpdateLayer: isVisible={isVisible}, isSimulationMode={isSimulationMode}");

            if (_bitmap == null)
            {
                Debug.WriteLine("MinimapDisplayRenderer.UpdateLayer: _bitmap is null, returning.");
                return;
            }

            // isVisible 和 isSimulationMode 的判斷主要由 ViewModel 控制何時調用此 UpdateLayer
            // Renderer 假定被調用時就應該繪製其內容（如果參數允許）

            _bitmap.Lock();
            try
            {
                Debug.WriteLine("MinimapDisplayRenderer.UpdateLayer: Locked bitmap. Clearing...");
                DrawingUtils.ClearBitmap(_bitmap); // 清除為透明

                // 在此繪製一個非常簡單且明顯的圖形，確保 Alpha 不是 0
                Color testFillColor = Colors.BlueViolet; // 使用一個 Alpha 為 255 的顏色
                DrawingUtils.FillRectangleOnBitmap(_bitmap, 0, 0, _pixelWidth, _pixelHeight, testFillColor);
                Debug.WriteLine($"MinimapDisplayRenderer.UpdateLayer: Filled with {testFillColor}.");

                // 再畫一個不同顏色的中心點
                Color centerMarkColor = Colors.Chartreuse; // 另一個 Alpha 為 255 的顏色
                int centerX = _pixelWidth / 2;
                int centerY = _pixelHeight / 2;
                DrawingUtils.DrawFilledCircleOnBitmap(_bitmap, centerX, centerY, 15, centerMarkColor);
                Debug.WriteLine($"MinimapDisplayRenderer.UpdateLayer: Drew center mark with {centerMarkColor}.");


                _bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _pixelWidth, _pixelHeight));
                Debug.WriteLine("MinimapDisplayRenderer.UpdateLayer: Added dirty rect. Unlocking.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MinimapDisplayRenderer.UpdateLayer: EXCEPTION - {ex.Message}");
                // 可以在此處設置一個錯誤標誌或日誌，以便更容易地發現問題
            }
            finally
            {
                _bitmap.Unlock();
            }
        }

        public void ClearLayer()
        {
            if (_bitmap != null)
            {
                Debug.WriteLine("MinimapDisplayRenderer.ClearLayer: Clearing bitmap.");
                _bitmap.Lock();
                try
                {
                    DrawingUtils.ClearBitmap(_bitmap); // 清除為透明
                    _bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _pixelWidth, _pixelHeight));
                }
                finally { _bitmap.Unlock(); }
            }
        }
    }
}