using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Serialization;

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
        public static FileAttributes GetFileAttrs(this FileCategories value)
            => value.GetAttribute<FileCategories, FileAttrsAttribute>()?.FileAttr
                ?? FileAttributes.Normal;
        #endregion

        internal static FileCategories GetCalcFileCategory(IFileSystemInfo fsInfo) =>
            Enum.GetValues<FileCategories>()
            .Reverse()
            .FirstOrDefault(x => fsInfo.Attributes.HasFlag(x.GetFileAttrs()));
    }
}
