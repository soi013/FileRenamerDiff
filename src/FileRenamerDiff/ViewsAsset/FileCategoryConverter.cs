﻿using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Data;

using FileRenamerDiff.Models;

using MaterialDesignThemes.Wpf;

namespace FileRenamerDiff.Views;

[ValueConversion(typeof(FileCategories), typeof(PackIconKind))]
public class FileCategoryToPackIconKindConverter : GenericConverter<FileCategories, PackIconKind>
{
    public override PackIconKind Convert(FileCategories category, object parameter, CultureInfo culture) =>
        category switch
        {
            FileCategories.HiddenFolder => PackIconKind.FolderHidden,
            FileCategories.HiddenFile => PackIconKind.FileHidden,
            FileCategories.Folder => PackIconKind.Folder,
            FileCategories.Image => PackIconKind.Image,
            FileCategories.Audio => PackIconKind.MusicNote,
            FileCategories.Video => PackIconKind.Video,
            FileCategories.Compressed => PackIconKind.FolderZip,
            FileCategories.Shortcut => PackIconKind.FileLink,
            FileCategories.Exe => PackIconKind.ApplicationCog,
            FileCategories.Library => PackIconKind.FileCog,
            FileCategories.Text => PackIconKind.FileDocument,
            FileCategories.Csv => PackIconKind.FileCsv,
            FileCategories.Word => PackIconKind.MicrosoftWord,
            FileCategories.Excel => PackIconKind.MicrosoftExcel,
            FileCategories.PowerPoint => PackIconKind.MicrosoftPowerpoint,
            FileCategories.Access => PackIconKind.MicrosoftAccess,
            FileCategories.OneNote => PackIconKind.MicrosoftOnenote,
            FileCategories.Outlook => PackIconKind.MicrosoftOutlook,
            FileCategories.Mail => PackIconKind.Email,
            FileCategories.Pdf => PackIconKind.FilePdfBox,
            FileCategories.Markdown => PackIconKind.LanguageMarkdown,

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
