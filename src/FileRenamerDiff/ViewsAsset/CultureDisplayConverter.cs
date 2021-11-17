using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace FileRenamerDiff.Views;

[ValueConversion(typeof(string), typeof(CultureInfo))]
public class CultureDisplayConverter : GenericConverter<CultureInfo, string>
{
    public override string Convert(CultureInfo selectCulture, object parameter, CultureInfo culture) =>
        selectCulture.Equals(CultureInfo.InvariantCulture)
            ? "-- Auto --"
            : $"{selectCulture.Name}/ {selectCulture.NativeName}/ {selectCulture.DisplayName}";

    public override CultureInfo ConvertBack(string cultureCode, object parameter, CultureInfo culture) =>
        CultureInfo.GetCultureInfo(cultureCode);
}
