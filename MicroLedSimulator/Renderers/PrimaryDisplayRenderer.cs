using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MicroLedSimulator.Utils;

namespace MicroLedSimulator.Renderers
{
    public class PrimaryDisplayRenderer
    {
        private WriteableBitmap? _bitmap;
        public ImageSource? BitmapSource => _bitmap;

        public PrimaryDisplayRenderer(int pixelWidth, int pixelHeight)
        {
            _bitmap = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32, null);
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
            if (_bitmap == null) return;
            _bitmap.Lock();
            try
            {
                DrawingUtils.ClearBitmap(_bitmap);
                if (!isSimulationMode) { _bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight)); return; }
                DrawBlueFilmEdgeInternal(_bitmap, blueFilmPositionX_mm, blueFilmPositionY_mm, blueFilmEdgeColor, blueFilmEdgeThickness, cameraPixelWidth_const, cameraPixelHeight_const, pixelsPerMmX_const, pixelsPerMmY_const, blueFilmRadiusMm_const);
                if (relativeDiePositions.Any())
                {
                    double dieWidthPx = currentDieWidthMm * pixelsPerMmX_const; double dieHeightPx = currentDieHeightMm * pixelsPerMmY_const;
                    foreach (var (relX_film_mm, relY_film_mm) in relativeDiePositions)
                    {
                        double dieCenterX_cam_mm = relX_film_mm - blueFilmPositionX_mm; double dieCenterY_cam_mm = relY_film_mm - blueFilmPositionY_mm;
                        double dieCanvasCenterX_px = (cameraPixelWidth_const / 2.0) + (dieCenterX_cam_mm * pixelsPerMmX_const); double dieCanvasCenterY_px = (cameraPixelHeight_const / 2.0) + (dieCenterY_cam_mm * pixelsPerMmY_const);
                        double dieCanvasLeft_px = dieCanvasCenterX_px - (dieWidthPx / 2.0); double dieCanvasTop_px = dieCanvasCenterY_px - (dieHeightPx / 2.0);
                        DrawingUtils.FillRectangleOnBitmap(_bitmap, (int)Math.Round(dieCanvasLeft_px), (int)Math.Round(dieCanvasTop_px), (int)Math.Round(dieWidthPx), (int)Math.Round(dieHeightPx), dieColor);
                    }
                }
                _bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally { _bitmap.Unlock(); }
        }
        private void DrawBlueFilmEdgeInternal(WriteableBitmap bmp, double blueFilmPositionX_mm, double blueFilmPositionY_mm, Color edgeColor, int edgeThickness, double cameraPixelWidth_const, double cameraPixelHeight_const, double pixelsPerMmX_const, double pixelsPerMmY_const, double blueFilmRadiusMm_const)
        {
            double filmCenterX_inCamera_mm = -blueFilmPositionX_mm; double filmCenterY_inCamera_mm = -blueFilmPositionY_mm;
            double filmCenterCanvasX_px = (cameraPixelWidth_const / 2.0) + (filmCenterX_inCamera_mm * pixelsPerMmX_const); double filmCenterCanvasY_px = (cameraPixelHeight_const / 2.0) + (filmCenterY_inCamera_mm * pixelsPerMmY_const);
            double filmRadiusCanvasX_px = blueFilmRadiusMm_const * pixelsPerMmX_const; double filmRadiusCanvasY_px = blueFilmRadiusMm_const * pixelsPerMmY_const;
            DrawingUtils.DrawCircleOutlineOnBitmap(bmp, (int)filmCenterCanvasX_px, (int)filmCenterCanvasY_px, (int)Math.Max(filmRadiusCanvasX_px, filmRadiusCanvasY_px), edgeColor, edgeThickness);
        }
        public void ClearLayer() { if (_bitmap != null) { _bitmap.Lock(); try { DrawingUtils.ClearBitmap(_bitmap); _bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight)); } finally { _bitmap.Unlock(); } } }
    }
}