using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    [ValueConversion(typeof(FileAttributes), typeof(PackIconKind))]
    public class FileAttributeToPackIconKindConverter : GenericConverter<FileAttributes, PackIconKind>
    {
        public override PackIconKind Convert(FileAttributes fileAttr, object parameter, CultureInfo culture) =>
            fileAttr.HasFlag(FileAttributes.Directory)
                ? PackIconKind.Folder
                : PackIconKind.File;

        public override FileAttributes ConvertBack(PackIconKind value, object parameter, CultureInfo culture) => default;
    }

    [ValueConversion(typeof(FileAttributes), typeof(string))]
    public class FileAttributeToStringConverter : GenericConverter<FileAttributes, string>
    {
        public override string Convert(FileAttributes fileAttr, object parameter, CultureInfo culture) =>
            fileAttr.HasFlag(FileAttributes.Directory)
            ? "Folder"
            : "File";

        public override FileAttributes ConvertBack(string value, object parameter, CultureInfo culture) => default;
    }
}
