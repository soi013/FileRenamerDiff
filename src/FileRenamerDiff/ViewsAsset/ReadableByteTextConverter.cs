using System.Globalization;
using System.Windows.Data;

namespace FileRenamerDiff.Views;

/// <summary>
/// 指定したByte数を見やすい文字列に変換する ex. 12,000 -> 12KB
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public class ReadableByteTextConverter : GenericConverter<long, string>
{
    public override string Convert(long lengthByte, object parameter, CultureInfo culture)
    {
        if (lengthByte < 0)
            return "-- B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (lengthByte >= 1024 && order < sizes.Length - 1)
        {
            order++;
            lengthByte /= 1024;
        }

        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
        // show a single decimal place, and no space.
        return string.Format("{0:0.##} {1}", lengthByte, sizes[order]);
    }

    public override long ConvertBack(string value, object parameter, CultureInfo culture) => default;
}
