using System.Windows.Media; // 用於 Brush

namespace MicroLedSimulator.Models
{
    public class DieModel
    {
        public double X { get; set; }       // Canvas 上的 X 像素位置
        public double Y { get; set; }       // Canvas 上的 Y 像素位置
        public double Width { get; set; }   // 像素寬度
        public double Height { get; set; }  // 像素高度
        public Brush Fill { get; set; } = Brushes.DodgerBlue; // 範例顏色
    }
}