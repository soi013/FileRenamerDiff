using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

using Anotar.Serilog;

using FileRenamerDiff.Properties;

using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

using Utf8Json;
using Utf8Json.Resolvers;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// アプリケーション設定クラス
    /// </summary>
    public class SettingAppModel : NotificationObject
    {
        static SettingAppModel()
        {
            JsonSerializer.SetDefaultResolver(StandardResolver.ExcludeNull);
        }

        /// <summary>
        /// 起動時に読み込むデフォルトファイルパス
        /// </summary>
        internal static readonly string DefaultFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            + $@"\{nameof(FileRenamerDiff)}\{nameof(FileRenamerDiff)}_Settings.json";

        private static readonly string myDocPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>
        /// リネームファイルを検索するターゲットパスリスト
        /// </summary>
        public IReadOnlyList<string> SearchFilePaths
        {
            get => _SearchFilePath;
            set => RaisePropertyChangedIfSet(ref _SearchFilePath, value, relatedProperty: nameof(ConcatedSearchFilePaths));
        }
        private IReadOnlyList<string> _SearchFilePath = new[] { "C:\\" };

        /// <summary>
        /// 連結されたリネームファイルを検索するターゲットパスリスト
        /// </summary>
        [IgnoreDataMember]
        public string ConcatedSearchFilePaths
        {
            get => SearchFilePaths.ConcatenateString('|');
            set => SearchFilePaths = value.Split('|');
        }

        /// <summary>
        /// 検索時に無視される拡張子コレクション
        /// </summary>
        public ObservableCollection<ValueHolder<string>> IgnoreExtensions { get; set; } =
            new[]
            {
                "pdb", "db", "cache","tmp","ini","DS_STORE",
            }
            .Select(x => ValueHolderFactory.Create(x))
            .ToObservableCollection();

        /// <summary>
        /// 検索時に無視される拡張子判定Regexの生成
        /// </summary>
        public Regex? CreateIgnoreExtensionsRegex()
        {
            var ignorePattern = IgnoreExtensions
                .Select(x => x.Value)
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .Select(x => $"^{x}$")
                .ConcatenateString('|');

            //無視する拡張子条件がない場合、逆にすべての拡張子にマッチしてしまうので、nullを返す
            return String.IsNullOrWhiteSpace(ignorePattern)
                ? null
                : AppExtention.CreateRegexOrNull(ignorePattern);
        }

        /// <summary>
        /// 削除文字列パターン
        /// </summary>
        public ObservableCollection<ReplacePattern> DeleteTexts { get; set; } =
            new ObservableCollection<ReplacePattern>
            {
                //Windowsでその場でコピーしたときの文字(- コピー)、OSの言語によって変わる
                new (Resources.Windows_CopyFileSuffix, ""),
                new (Resources.Windows_ShortcutFileSuffix, ""),
                //Windowsの(数字)タグ文字
                new (@"\s*\([0-9]{0,3}\)", "",true),
            };

        /// <summary>
        /// 置換文字列パターン
        /// </summary>
        public ObservableCollection<ReplacePattern> ReplaceTexts { get; set; } =
            new ObservableCollection<ReplacePattern>
            {
                new (".jpeg$", ".jpg", true),
                new (".htm$", ".html", true),
                //2以上の全・半角スペースを1つのスペースに変更
                new ("\\s+", " ", true),
            };

        /// <summary>
        /// ファイル探索時にサブディレクトリを探索するか
        /// </summary>
        public bool IsSearchSubDirectories
        {
            get => _IsSearchSubDirectories;
            set => RaisePropertyChangedIfSet(ref _IsSearchSubDirectories, value);
        }
        private bool _IsSearchSubDirectories = true;

        /// <summary>
        /// 隠しファイルをリネーム対象にするか
        /// </summary>
        public bool IsHiddenRenameTarget
        {
            get => _IsHiddenRenameTarget;
            set => RaisePropertyChangedIfSet(ref _IsHiddenRenameTarget, value);
        }
        private bool _IsHiddenRenameTarget = false;

        /// <summary>
        /// ディレクトリをリネーム対象にするか
        /// </summary>
        public bool IsDirectoryRenameTarget
        {
            get => _IsDirectoryRenameTarget;
            set => RaisePropertyChangedIfSet(ref _IsDirectoryRenameTarget, value);
        }
        private bool _IsDirectoryRenameTarget = true;

        /// <summary>
        /// ディレクトリでないファイルをリネーム対象にするか
        /// </summary>
        public bool IsFileRenameTarget
        {
            get => _IsFileRenameTarget;
            set => RaisePropertyChangedIfSet(ref _IsFileRenameTarget, value);
        }
        private bool _IsFileRenameTarget = true;

        /// <summary>
        /// リネーム対象になるファイル種類がないか
        /// </summary>
        public bool IsNoFileRenameTarget() => !IsDirectoryRenameTarget && !IsFileRenameTarget;

        /// <summary>
        /// リネーム対象となるファイル種類か
        /// </summary>
        public bool IsTargetFile(IFileSystem fileSystem, string filePath)
        {
            var fileAttributes = fileSystem.File.GetAttributes(filePath);
            //隠しファイルが対象外の設定で、隠しファイルだったらリネームしない
            if (!IsHiddenRenameTarget && fileAttributes.HasFlag(FileAttributes.Hidden))
                return false;

            //ディレクトリが対象外の設定で、ディレクトリだったらリネームしない
            if (!IsDirectoryRenameTarget && fileAttributes.HasFlag(FileAttributes.Directory))
                return false;

            //ファイルが対象外の設定で、ファイルだったらリネームしない
            if (!IsFileRenameTarget && !fileAttributes.HasFlag(FileAttributes.Directory))
                return false;

            //それ以外はリネームする
            return true;
        }

        /// <summary>
        /// 現在の設定をもとに、ファイル探索時にスキップする属性を取得
        /// </summary>
        public FileAttributes GetSkipAttribute()
        {
            var fa = FileAttributes.System;

            if (!IsHiddenRenameTarget)
                fa |= FileAttributes.Hidden;

            //サブディレクトリを探索せず、ディレクトリ自体もリネーム対象でないなら、スキップ
            if (!IsDirectoryRenameTarget && !IsSearchSubDirectories)
                fa |= FileAttributes.Directory;

            if (!IsFileRenameTarget)
                fa |= FileAttributes.Normal;

            return fa;
        }

        /// <summary>
        /// リネーム時に拡張子を無視するか
        /// </summary>
        public bool IsRenameExt
        {
            get => _IsIgnoreExt;
            set => RaisePropertyChangedIfSet(ref _IsIgnoreExt, value);
        }
        private bool _IsIgnoreExt = true;

        /// <summary>
        /// アプリケーションの表示言語
        /// </summary>
        public string AppLanguageCode
        {
            get => _AppLanguageCode;
            set => RaisePropertyChangedIfSet(ref _AppLanguageCode, value);
        }
        private string _AppLanguageCode = "";

        /// <summary>
        /// アプリケーションの色テーマ
        /// </summary>
        public bool IsAppDarkTheme
        {
            get => _IsAppDarkTheme;
            set => RaisePropertyChangedIfSet(ref _IsAppDarkTheme, value);
        }
        private bool _IsAppDarkTheme = true;

        /// <summary>
        /// 変更時に変更前後の履歴を保存するか
        /// </summary>
        public bool IsCreateRenameLog
        {
            get => _IsCreateRenameLog;
            set => RaisePropertyChangedIfSet(ref _IsCreateRenameLog, value);
        }
        private bool _IsCreateRenameLog;

        internal void AddIgnoreExtensions() => IgnoreExtensions.Add(ValueHolderFactory.Create(String.Empty));

        internal void AddDeleteTexts() => DeleteTexts.Add(ReplacePattern.CreateEmpty());

        internal void AddReplaceTexts() => ReplaceTexts.Add(ReplacePattern.CreateEmpty());

        /// <summary>
        /// ファイルから設定ファイルをデシリアライズ
        /// </summary>
        public static SettingAppModel Deserialize(IFileSystem fileSystem, string settingFilePath)
        {
            using var fileStream = fileSystem.FileStream.Create(settingFilePath, FileMode.Open);
            return JsonSerializer.Deserialize<SettingAppModel>(fileStream);
        }

        /// <summary>
        /// ファイルに設定ファイルをシリアライズ
        /// </summary>
        public void Serialize(IFileSystem fileSystem, string settingFilePath)
        {
            string? dirPath = Path.GetDirectoryName(settingFilePath);
            if (!String.IsNullOrWhiteSpace(dirPath))
                fileSystem.Directory.CreateDirectory(dirPath);

            using var fileStream = fileSystem.FileStream.Create(settingFilePath, FileMode.Create);

            JsonSerializer.Serialize(fileStream, this);
        }
    }
}
