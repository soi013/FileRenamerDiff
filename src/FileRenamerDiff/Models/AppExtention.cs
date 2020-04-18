using DiffPlex.DiffBuilder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace FileRenamerDiff.Models
{
    public static class AppExtention
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source) => new ObservableCollection<T>(source);

        public static string ConcatenateString<T>(this IEnumerable<T> values, string sepalator) => String.Join(sepalator, values);
        public static string ConcatenateString<T>(this IEnumerable<T> values, char sepalator) => String.Join(sepalator, values);
        public static string ToRawText(this DiffPaneModel diffPane) => $"{diffPane.Lines.ConcatenateString(Environment.NewLine)}";
        public static string ToDisplayString(this SideBySideDiffModel ssDiff) => $"{ssDiff.OldText.ToRawText()}->{ssDiff.NewText.ToRawText()}";

        public static T ToFreeze<T>(this T obj) where T : Freezable
        {
            obj.Freeze();
            return obj;
        }
        public static string ToCode(this Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        public static Color CodeToColorOrTransparent(string code) => CodeToColor(code) ?? Colors.Transparent;
        public static Color? CodeToColor(string code)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(code);
            }
            catch (FormatException ex)
            {
                Debug.WriteLine($"{ex.Message}");
                return null;
            }
        }
    }
}