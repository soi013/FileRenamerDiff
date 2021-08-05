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

using Anotar.Serilog;

using FileRenamerDiff.Models;

using Livet.Messaging;

using MaterialDesignThemes.Wpf;

using Serilog.Events;

namespace FileRenamerDiff.Views
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToBrushConverter : GenericConverter<bool, Brush>
    {
        private static readonly Brush normalBrush = Colors.Transparent.ToSolidColorBrush(true);

        public Brush? TrueBrush { get; set; }
        public Brush? FalseBrush { get; set; }

        public override Brush Convert(bool value, object parameter, CultureInfo culture) =>
            (value ? TrueBrush : FalseBrush)
            ?? normalBrush;

        public override bool ConvertBack(Brush value, object parameter, CultureInfo culture) => default;
    }
}
