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
    [ValueConversion(typeof(LogEventLevel), typeof(PackIconKind))]
    public class LogEventLevelToPackIconKindConverter : GenericConverter<LogEventLevel, PackIconKind>
    {
        public override PackIconKind Convert(LogEventLevel level, object parameter, CultureInfo culture) =>
            level switch
            {
                LogEventLevel.Warning => PackIconKind.Alert,
                LogEventLevel.Error => PackIconKind.AlertOctagram,
                LogEventLevel.Fatal => PackIconKind.CloseOctagon,
                _ => PackIconKind.Information
            };

        public override LogEventLevel ConvertBack(PackIconKind value, object parameter, CultureInfo culture) => default;
    }


    [ValueConversion(typeof(LogEventLevel), typeof(Brush))]
    public class LogEventLevelToBrushConverter : GenericConverter<LogEventLevel, Brush>
    {
        private static readonly Brush normalBrush = (SolidColorBrush)App.Current.Resources["MaterialDesignBody"];
        private static readonly Brush alertBrush = Colors.Orange.ToSolidColorBrush(true);
        private static readonly Brush errorBrush = Colors.Red.ToSolidColorBrush(true);

        public override Brush Convert(LogEventLevel level, object parameter, CultureInfo culture)
        {
            return level switch
            {
                LogEventLevel.Warning => alertBrush,
                LogEventLevel.Error => errorBrush,
                LogEventLevel.Fatal => errorBrush,
                _ => normalBrush,
            };
        }

        public override LogEventLevel ConvertBack(Brush value, object parameter, CultureInfo culture) => default;
    }
}
