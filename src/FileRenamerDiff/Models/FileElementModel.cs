using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Resources;
using System.Globalization;
using System.Windows.Data;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using Reactive.Bindings;
using System.Reactive;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using Anotar.Serilog;

using FileRenamerDiff.Properties;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// リネーム処理状態
    /// </summary>
    public enum RenameState
    {
        /// <summary>
        /// 未リネーム
        /// </summary>
        None,
        /// <summary>
        /// リネーム済み
        /// </summary>
        Renamed,
        /// <summary>
        /// リネーム失敗
        /// </summary>
        FailedToRename,
    }

    /// <summary>
    /// リネーム前後のファイル名を含むファイル情報モデル
    /// </summary>
    public class FileElementModel : NotificationObject
    {
        private readonly bool isDirectory;
        private readonly FileSystemInfo fsInfo;

        /// <summary>
        /// リネーム前 フルファイルパス
        /// </summary>
        public string InputFilePath => fsInfo.FullName;

        /// <summary>
        /// リネーム前 ファイル名
        /// </summary>
        public string InputFileName => fsInfo.Name;

        private string outputFileName = "--.-";
        /// <summary>
        /// リネーム後 ファイル名
        /// </summary>
        public string OutputFileName
        {
            get => outputFileName;
            set => RaisePropertyChangedIfSet(ref outputFileName, value, new[] { nameof(IsReplaced), nameof(OutputFilePath) });
        }

        /// <summary>
        /// リネーム後 ファイルパス
        /// </summary>
        public string OutputFilePath => Path.Combine(DirectoryPath, outputFileName);

        /// <summary>
        /// リネーム前後で変更があったか
        /// </summary>
        public bool IsReplaced => InputFileName != OutputFileName;

        private bool _IsConflicted;
        /// <summary>
        /// 他のファイルパスと衝突しているか（上位層で判定する）
        /// </summary>
        public bool IsConflicted
        {
            get => _IsConflicted;
            set => RaisePropertyChangedIfSet(ref _IsConflicted, value);
        }

        /// <summary>
        /// ファイルの所属しているディレクトリ名
        /// </summary>
        public string DirectoryPath => fsInfo.GetDirectoryPath() ?? string.Empty;

        /// <summary>
        /// ファイルのバイト数 （ディレクトリの場合は-1B）
        /// </summary>
        public long LengthByte => (fsInfo.Exists && !isDirectory) ? ((FileInfo)fsInfo).Length : -1;

        /// <summary>
        /// ファイル更新日時
        /// </summary>
        public DateTime LastWriteTime => fsInfo.LastWriteTime;

        /// <summary>
        /// ファイル作成日時
        /// </summary>
        public DateTime CreationTime => fsInfo.CreationTime;

        /// <summary>
        /// ファイル属性
        /// </summary>
        public FileAttributes Attributes => fsInfo.Attributes;


        private RenameState _State = RenameState.None;
        /// <summary>
        /// リネーム状態
        /// </summary>
        public RenameState State
        {
            get => _State;
            set => RaisePropertyChangedIfSet(ref _State, value);
        }

        private string _PreviousInputFilePath;
        /// <summary>
        /// Rename前のファイルパス
        /// </summary>
        public string PreviousInputFilePath
        {
            get => _PreviousInputFilePath;
            set => RaisePropertyChangedIfSet(ref _PreviousInputFilePath, value);
        }

        /// <summary>
        /// 存在するか
        /// </summary>
        public bool Exists => fsInfo.GetExistWithReflesh();

        /// <summary>
        /// ファイル名に指定できない文字の検出器 (参考:https://dobon.net/vb/dotnet/file/invalidpathchars.html#section2)
        /// </summary>
        private static readonly Regex invalidCharRegex = new(
        "[\\x00-\\x1f<>:\"/\\\\|?*]" +
        "|^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9]|CLOCK\\$)(\\.|$)" +
        "|[\\. ]$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public FileElementModel(string filePath)
        {
            this.isDirectory = File.GetAttributes(filePath)
                .HasFlag(FileAttributes.Directory);

            this.fsInfo = isDirectory
                ? new DirectoryInfo(filePath)
                : new FileInfo(filePath);

            this.outputFileName = InputFileName;
            this._PreviousInputFilePath = InputFilePath;
        }

        /// <summary>
        /// 指定された置換パターンで、ファイル名を置換する（ストレージに保存はされない）
        /// </summary>
        internal void Replace(IReadOnlyList<ReplaceRegex> repRegexes, bool isRenameExt)
        {
            //設定によって拡張子をリネームするかを決定
            string outFileName = isRenameExt || isDirectory
                ? InputFileName
                : Path.GetFileNameWithoutExtension(InputFileName);

            foreach (var reg in repRegexes)
            {
                outFileName = reg.Replace(outFileName);
            }

            //拡張子をリネームしない設定であったら、元の拡張子文字列を連結
            if (!isRenameExt && !isDirectory)
                outFileName += fsInfo.Extension;

            //ファイル名に指定できない文字は"_"に置き換える
            if (invalidCharRegex.IsMatch(outFileName))
            {
                LogTo.Warning("Invalid Char included {@outFileName}", outFileName);
                Model.Instance.MessageEvent.Value = new AppMessage(AppMessageLevel.Alert,
                    head: Resources.Alert_InvalidFileName,
                   body: $"{InputFileName} -> {outFileName}");

                outFileName = invalidCharRegex.Replace(outFileName, "_");
            }

            OutputFileName = outFileName;

            if (IsReplaced)
                LogTo.Debug("Replaced {@Input} -> {@Output} in {@DirectoryPath}", InputFileName, OutputFileName, DirectoryPath);
        }

        /// <summary>
        /// リネームを実行（ストレージに保存される）
        /// </summary>
        internal void Rename()
        {
            try
            {
                LogTo.Debug("Save {@Input} -> {@Output} in {@DirectoryPath}", InputFileName, OutputFileName, DirectoryPath);
                PreviousInputFilePath = InputFilePath;
                fsInfo.Rename(OutputFilePath);
                fsInfo.Refresh();
                //rename時にFileInfoが変更されるので、通知を上げておく
                foreach (var name in new[] { nameof(InputFileName), nameof(InputFilePath), nameof(IsReplaced), nameof(Exists), })
                    RaisePropertyChanged(name);

                State = RenameState.Renamed;
            }
            catch (Exception ex)
            {
                LogTo.Warning(ex, "Fail to Rename {@fileElement}", this);
                State = RenameState.FailedToRename;
            }
        }
        public override string ToString() => $"{InputFileName}->{OutputFileName}";
    }
}