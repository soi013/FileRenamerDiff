using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using FileRenamerDiff.Models;

using MaterialDesignThemes.Wpf;

namespace FileRenamerDiff.Views
{
    [ValueConversion(typeof(FileCategories), typeof(PackIconKind))]
    public class FileCategoryToPackIconKindConverter : GenericConverter<FileCategories, PackIconKind>
    {
        public override PackIconKind Convert(FileCategories category, object parameter, CultureInfo culture) =>
            category switch
            {
                FileCategories.HiddenFolder => PackIconKind.Folder,//PackIconにHiddenFolderが追加されたら変更
                FileCategories.HiddenFile => PackIconKind.FileHidden,
                FileCategories.Folder => PackIconKind.Folder,
                FileCategories.Image => PackIconKind.Image,
                FileCategories.Audio => PackIconKind.MusicNote,

                _ => PackIconKind.FileOutline
            };

        public override FileCategories ConvertBack(PackIconKind value, object parameter, CultureInfo culture) => default;
    }

    [ValueConversion(typeof(FileCategories), typeof(string))]
    public class FileCategoryToStringConverter : GenericConverter<FileCategories, string>
    {
        public override string Convert(FileCategories category, object parameter, CultureInfo culture) =>
           category.GetAttribute<FileCategories, EnumMemberAttribute>()?.Value
            ?? category.ToString();

        public override FileCategories ConvertBack(string value, object parameter, CultureInfo culture) => default;
    }
}
