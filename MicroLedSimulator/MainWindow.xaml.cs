using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
// using MicroLedSimulator.Models; // 如果 EnumComboBoxHelper 需要

namespace MicroLedSimulator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private static readonly Regex _doubleRegex = new Regex(@"^-?[0-9]*\.?[0-9]*$");
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox? textBox = sender as TextBox; if (textBox == null) return;
            string currentText = textBox.Text; string futureText = currentText.Insert(textBox.CaretIndex, e.Text);
            if (!_doubleRegex.IsMatch(futureText)) { e.Handled = true; return; }
            if (e.Text == "." && currentText.Contains(".")) { e.Handled = true; return; }
            if (e.Text == "-") { if (currentText.Contains("-") || textBox.CaretIndex != 0) { e.Handled = true; return; } }
        }
    }

    public static class EnumComboBoxHelper
    {
        private static IEnumerable<object> GetEnumValuesForDisplay(Type enumType)
        {
            if (!enumType.IsEnum) throw new ArgumentException("類型必須是列舉。");
            return Enum.GetValues(enumType).Cast<object>()
                       .Select(e => new { Value = e, Display = e.ToString() }).ToList();
        }
        public static IEnumerable<object> CameraModeItemsSource => GetEnumValuesForDisplay(typeof(MicroLedSimulator.Models.CameraMode));
    }
}