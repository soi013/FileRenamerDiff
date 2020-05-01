using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using Anotar.Serilog;
using Livet;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// リネーム前後のファイル名を含むファイル情報モデル
    /// </summary>
    public class FileElementModel : NotificationObject
    {
        private Model model => Model.Instance;
        private SettingAppModel settingApp => model.Setting;

        private readonly string path;
        private readonly FileInfo fileInfo;

        /// <summary>
        /// リネーム前 ファイル名
        /// </summary>
        public string InputFileName => fileInfo?.Name;

        private string outputFileName = "--.-";
        /// <summary>
        /// リネーム後 ファイル名
        /// </summary>
        public string OutputFileName
        {
            get => outputFileName;
            set => RaisePropertyChangedIfSet(ref outputFileName, value, nameof(IsReplaced));
        }

        private string replacedPath => Path.Combine(DirectoryPath, outputFileName);

        /// <summary>
        /// リネーム前後で変更があったか
        /// </summary>
        public bool IsReplaced => InputFileName != OutputFileName;

        /// <summary>
        /// ファイルの所属しているディレクトリ名
        /// </summary>
        public string DirectoryPath => fileInfo.DirectoryName;

        /// <summary>
        /// ファイルのバイト数 （ディレクトリの場合は-1B）
        /// </summary>
        public long LengthByte => fileInfo.Exists ? fileInfo.Length : -1;

        /// <summary>
        /// ファイル更新日時
        /// </summary>
        public DateTime LastWriteTime => fileInfo.LastWriteTime;

        /// <summary>
        /// ファイル作成日時
        /// </summary>
        public DateTime CreationTime => fileInfo.CreationTime;

        /// <summary>
        /// ファイル属性
        /// </summary>
        public FileAttributes Attributes => fileInfo.Attributes;

        public FileElementModel(string path)
        {
            this.path = path;

            this.fileInfo = new FileInfo(path);

            this.outputFileName = InputFileName;
        }

        /// <summary>
        /// 指定された
        /// </summary>
        /// <param name="repRegexes"></param>
        internal void Replace(IReadOnlyList<ReplaceRegex> repRegexes)
        {
            var outFileName = InputFileName;

            foreach (var reg in repRegexes)
            {
                outFileName = reg.Replace(outFileName);
            }

            OutputFileName = outFileName;
            LogTo.Debug("Replaced {@Input} -> {@Output} in {@DirectoryPath}", InputFileName, OutputFileName, DirectoryPath);
        }

        public override string ToString() => $"{InputFileName}->{OutputFileName}";

        internal void Rename()
        {
            LogTo.Debug("Save {@Input} -> {@Output} in {@DirectoryPath}", InputFileName, OutputFileName, DirectoryPath);
            if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                Directory.Move(this.path, this.replacedPath);
            else
                fileInfo.MoveTo(replacedPath);
        }
    }
}