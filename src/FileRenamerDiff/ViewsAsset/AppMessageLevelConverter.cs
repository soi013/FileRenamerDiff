using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Globalization;

using Anotar.Serilog;
using Serilog.Events;
using Livet.Messaging;
using MaterialDesignThemes.Wpf;

using FileRenamerDiff.Models;
using System.Windows.Controls;

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
                _ => PackIconKind.Information
            };

        public override AppMessageLevel ConvertBack(PackIconKind value, object parameter, CultureInfo culture) => default;
    }


    [ValueConversion(typeof(AppMessageLevel), typeof(Brush))]
    public class LogEventLevelToBrushConverter : GenericConverter<AppMessageLevel, Brush>
    {
        private static readonly Brush normalBrush = (SolidColorBrush)App.Current.Resources["MaterialDesignBody"];
        private static readonly Brush alertBrush = Colors.Orange.ToSolidColorBrush(true);
        private static readonly Brush errorBrush = Colors.Red.ToSolidColorBrush(true);

        public override Brush Convert(AppMessageLevel level, object parameter, CultureInfo culture)
        {
            return level switch
            {
                AppMessageLevel.Alert => alertBrush,
                AppMessageLevel.Error => errorBrush,
                _ => normalBrush,
            };
        }

        public override AppMessageLevel ConvertBack(Brush value, object parameter, CultureInfo culture) => default;
    }
}
