using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace FileRenamerDiff.Views
{

    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToCursorConverter : GenericConverter<bool, String>
    {
        public override string Convert(bool isIdle, object parameter, CultureInfo culture) =>
            (isIdle ? null : Cursors.Wait)?.ToString();

        public override bool ConvertBack(String value, object parameter, CultureInfo culture) => false;
    }

}
