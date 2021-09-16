using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

using static FileRenamerDiff.Models.FileCategoriesExt;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// ファイル種類
    /// </summary>
    public enum FileCategories
    {
        /// <summary>
        /// 通常ファイル
        /// </summary>
        [EnumMember(Value = "Normal other file")]
        OtherFile,

        /// <summary>
        /// テキストファイル
        /// </summary>
        [EnumMember(Value = "Comma, tab, or character file")]
        [FileExtPattern("csv", "tsv")]
        Csv,
        /// <summary>
        /// テキストファイル
        /// </summary>
        [EnumMember(Value = "Text file")]
        [FileExtPattern("txt")]
        Text,
        /// <summary>
        /// ライブラリファイル
        /// </summary>
        [EnumMember(Value = "Library file")]
        [FileExtPattern("dll", "ocx", "sys", "so", "dylib", "bundle")]
        Library,
        /// <summary>
        /// 実行ファイル
        /// </summary>
        [EnumMember(Value = "Executable file")]
        [FileExtPattern("exe", "com", "app")]
        Exe,
        /// <summary>
        /// ショートカットファイル
        /// </summary>
        [EnumMember(Value = "Shortcut file")]
        [FileExtPattern("lnk")]
        Shortcut,
        /// <summary>
        /// 圧縮ファイル
        /// </summary>
        [EnumMember(Value = "Compressed file")]
        [FileExtPattern("zip", "tar", "lzh", "cab", "gz", "gzip", "rar", "7z", "xz", "txz", "taz", "tgz", "lha", "xar")]
        //[FileAttrs(FileAttributes.Compressed)] //FileAttributes.Compressedは圧縮ファイルではなく、Windowsが「内容を圧縮してディスク領域を節約する」の対象にしているかを示す
        Compressed,
        /// <summary>
        /// 動画ファイル
        /// </summary>
        [EnumMember(Value = "Video file")]
        [FileExtPattern("mp4", "m4v", "3gp", "3g2", "mov", "qt", "avi", "wmv", "div", "divx", "ts", "mts", "m2ts", "m2t", "vob", "mkv", "3gp", "3g2", "webm", "rm", "rmvb", "ram")]
        Video,
        /// <summary>
        /// 音声ファイル
        /// </summary>
        [EnumMember(Value = "Sound file")]
        [FileExtPattern("mp3", "mid", "midi", "wav", "wave", "aif", "aiff", "aifc", "au", "snd", "flac", "aac", "m4a", "m4p", "wma", "mka", "omg", "oma", "aa3", "opus", "ogg", "oga", "asf", "alac")]
        Audio,
        /// <summary>
        /// 画像ファイル
        /// </summary>
        [EnumMember(Value = "Image file")]
        [FileExtPattern("gif", "jpg", "jpeg", "jpe", "jfif", "pjpeg", "pjp", "png", "svg", "bmp", "dib", "rle", "ico", "ai", "art", "cam", "cdr", "cgm", "cmp", "dpx", "fal", "q0", "fpx", "j6i", "mac", "mag", "maki", "mng", "pcd", "pct", "pic", "pict", "pcx", "pmp", "pnm", "psd", "ras", "sj1", "tif", "tiff", "nsk", "tga", "wmf", "wpg", "xbm", "xpm", "eps", "epsf", "heic", "heif")]
        Image,
        /// <summary>
        /// 通常フォルダ
        /// </summary>
        [EnumMember(Value = "Normal folder")]
        [FileAttrs(FileAttributes.Directory)]
        Folder,
        /// <summary>
        /// 隠しファイル
        /// </summary>
        [EnumMember(Value = "Hidden file")]
        [FileAttrs(FileAttributes.Hidden)]
        HiddenFile,
        /// <summary>
        /// 隠しフォルダ
        /// </summary>
        [EnumMember(Value = "Hidden folder")]
        [FileAttrs(FileAttributes.Hidden | FileAttributes.Directory)]
        HiddenFolder,
    }

    public static class FileCategoriesExt
    {
        #region FileAttrs属性
        /// <summary>
        /// FileAttrs属性
        /// </summary>
        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        public sealed class FileAttrsAttribute : Attribute
        {
            public FileAttributes FileAttr { get; private set; }

            public FileAttrsAttribute(FileAttributes fileAttr)
            {
                this.FileAttr = fileAttr;
            }
        }
        /// <summary>
        /// FileAttrs属性の取得
        /// </summary>
        public static FileAttributes? GetFileAttrs(this FileCategories value)
            => value.GetAttribute<FileCategories, FileAttrsAttribute>()?.FileAttr;
        #endregion

        internal static FileCategories GetCalcFileCategory(IFileSystemInfo fsInfo) =>
            Enum.GetValues<FileCategories>()
            .Reverse()
            .FirstOrDefault(x => IsCategory(fsInfo, x));

        private static bool IsCategory(IFileSystemInfo fsInfo, FileCategories category)
        {
            FileAttributes? attrs = category.GetFileAttrs();

            if (attrs is not null)
                return fsInfo.Attributes.HasFlag((FileAttributes)attrs);

            string fileExt = AppExtension.GetExtentionCoreFromPath(fsInfo.Name).ToLowerInvariant();
            return category.GetFileExtPattern().Contains(fileExt);
        }

        #region FileExtPattern属性
        /// <summary>
        /// FileExtPattern属性
        /// </summary>
        [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
        public sealed class FileExtPatternAttribute : Attribute
        {
            public string[] FileExtPattern { get; private set; }

            public FileExtPatternAttribute(params string[] fileExtPattern)
            {
                this.FileExtPattern = fileExtPattern;
            }
        }
        /// <summary>
        /// FileExtPattern属性の取得
        /// </summary>
        public static string[] GetFileExtPattern(this FileCategories value)
            => value.GetAttribute<FileCategories, FileExtPatternAttribute>()?.FileExtPattern
                ?? Array.Empty<string>();
        #endregion
    }
}
