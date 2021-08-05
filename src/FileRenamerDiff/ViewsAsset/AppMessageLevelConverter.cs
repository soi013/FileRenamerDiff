using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using FileRenamerDiff.Models;

using MaterialDesignThemes.Wpf;

namespace FileRenamerDiff.Views
{
    [ValueConversion(typeof(AppMessageLevel), typeof(PackIconKind))]
    public class LogEventLevelToPackIconKindConverter : GenericConverter<AppMessageLevel, PackIconKind>
    {
        public override PackIconKind Convert(AppMessageLevel level, object parameter, CultureInfo culture) =>
            level switch
            {
                AppMessageLevel.Alert => PackIconKind.Alert,
                AppMessageLevel.Error => PackIconKind.AlertOctagram,
                AppMessageLevel.Info => PackIconKind.Information,
                _ => PackIconKind.Information
            };

        public override AppMessageLevel ConvertBack(PackIconKind value, object parameter, CultureInfo culture) => default;
    }

    [ValueConversion(typeof(AppMessageLevel), typeof(Brush))]
    public class LogEventLevelToBrushConverter : GenericConverter<AppMessageLevel, Brush>
    {
        private static readonly Brush normalBrush = App.Current.Resources["MaterialDesignBody"] as SolidColorBrush
            ?? Colors.White.ToSolidColorBrush(true);

        private static readonly Brush alertBrush = Colors.Orange.ToSolidColorBrush(true);
        private static readonly Brush errorBrush = Colors.Red.ToSolidColorBrush(true);

        public override Brush Convert(AppMessageLevel level, object parameter, CultureInfo culture) =>
            level switch
            {
                AppMessageLevel.Alert => alertBrush,
                AppMessageLevel.Error => errorBrush,
                _ => normalBrush,
            };

        public override AppMessageLevel ConvertBack(Brush value, object parameter, CultureInfo culture) => default;
    }
}
