using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using FileRenamerDiff.Models;

namespace FileRenamerDiff.Views;

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
