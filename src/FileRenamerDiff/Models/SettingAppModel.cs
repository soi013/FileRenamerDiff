using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Anotar.Serilog;
using Livet;
using Reactive.Bindings;
using MessagePack;
using MessagePack.ReactivePropertyExtension;
using MessagePack.Resolvers;

using FileRenamerDiff.Properties;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// アプリケーション設定クラス
    /// </summary>
    public class SettingAppModel : NotificationObject
    {
        static SettingAppModel()
        {
            //ReactivePropertyをシリアライズ可能にするため、アプリケーション全体で固定のMessagePackResolverを設定
            var resolver = CompositeResolver.Create(
                ReactivePropertyResolver.Instance,
                ContractlessStandardResolverAllowPrivate.Instance
            );
            MessagePackSerializer.DefaultOptions = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        internal static readonly string SettingFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            + $@"\{nameof(FileRenamerDiff)}\{nameof(SettingAppModel)}.json";

        private static readonly string myDocPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>
        /// リネームファイルを検索するターゲットパス
        /// </summary>
        public ReactivePropertySlim<string> SearchFilePath =>
            searchFilePath ??= new ReactivePropertySlim<string>("");
        private ReactivePropertySlim<string> searchFilePath;

        /// <summary>
        /// 検索時に無視される拡張子コレクション
        /// </summary>
        public ObservableCollection<ReactivePropertySlim<string>> IgnoreExtensions =>
            ignoreExtensions ??= new[]
            {
                "pdb", "db", "cache","tmp","ini",
            }
            .Select(x => new ReactivePropertySlim<string>(x))
            .ToObservableCollection();
        private ObservableCollection<ReactivePropertySlim<string>> ignoreExtensions;

        /// <summary>
        /// 検索時に無視される拡張子判定Regexの生成
        /// </summary>
        public Regex CreateIgnoreExtensionsRegex()
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
        public ObservableCollection<ReplacePattern> DeleteTexts =>
            deleteTexts ??= new[]
            {
                //Windowsでその場でコピーしたときの文字(- コピー)、OSの言語によって変わる
                Resources.Windows_CopyFilePostFix,
                "(1)",
                "(2)",
                "(3)",
                "(4)",
                "(5)",
                "(6)",
                "(7)",
                "(8)",
                "(9)",
            }
            .Select(x => new ReplacePattern(x, ""))
            .ToObservableCollection();
        private ObservableCollection<ReplacePattern> deleteTexts;

        /// <summary>
        /// 置換文字列パターン
        /// </summary>
        public ObservableCollection<ReplacePattern> ReplaceTexts =>
            replaceTexts ??= new ObservableCollection<ReplacePattern>
            {
                new ReplacePattern(".jpeg$", ".jpg", true),
                new ReplacePattern(".htm$", ".html", true),
                //2以上の全・半角スペースを1つのスペースに変更
                new ReplacePattern("\\s+", " ", true),
            };
        private ObservableCollection<ReplacePattern> replaceTexts;

        /// <summary>
        /// ファイル探索時にサブディレクトリを探索するか
        /// </summary>
        public ReactivePropertySlim<bool> IsSearchSubDirectories =>
           isSearchSubDirectories ??= new ReactivePropertySlim<bool>(true);
        private ReactivePropertySlim<bool> isSearchSubDirectories;

        /// <summary>
        /// 隠しファイルをリネーム対象にするか
        /// </summary>
        public ReactivePropertySlim<bool> IsHiddenRenameTarget =>
            isHiddenRenameTarget ??= new ReactivePropertySlim<bool>(false);
        private ReactivePropertySlim<bool> isHiddenRenameTarget;

        /// <summary>
        /// ディレクトリをリネーム対象にするか
        /// </summary>
        public ReactivePropertySlim<bool> IsDirectoryRenameTarget =>
            isDirectoryRenameTarget ??= new ReactivePropertySlim<bool>(true);
        private ReactivePropertySlim<bool> isDirectoryRenameTarget;

        /// <summary>
        /// ディレクトリでないファイルをリネーム対象にするか
        /// </summary>
        public ReactivePropertySlim<bool> IsFileRenameTarget =>
            isFileRenameTarget ??= new ReactivePropertySlim<bool>(true);
        private ReactivePropertySlim<bool> isFileRenameTarget;

        /// <summary>
        /// リネーム対象になるファイル種類がないか
        /// </summary>
        public bool IsNoFileRenameTarget() => !IsDirectoryRenameTarget.Value && !IsFileRenameTarget.Value;

        /// <summary>
        /// リネーム対象となるファイル種類か
        /// </summary>
        public bool IsTargetFile(FileInfo fileInfo)
        {
            if (!IsHiddenRenameTarget.Value && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                return false;

            if (!IsDirectoryRenameTarget.Value && fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                return false;

            if (!IsFileRenameTarget.Value && !fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                return false;

            return true;
        }

        /// <summary>
        /// アプリケーションの表示言語
        /// </summary>
        public ReactivePropertySlim<string> AppLanguageCode =>
            appLanguage ??= new ReactivePropertySlim<string>("");
        private ReactivePropertySlim<string> appLanguage;

        /// <summary>
        /// アプリケーションの色テーマ
        /// </summary>
        public ReactivePropertySlim<bool> IsAppDarkTheme =>
            isAppDarkTheme ??= new ReactivePropertySlim<bool>(true);
        private ReactivePropertySlim<bool> isAppDarkTheme;

        public SettingAppModel()
        {
            AppLanguageCode.Subscribe(x => LogTo.Information("Change Lang {@lang}", x));
        }

        internal void AddIgnoreExtensions() => IgnoreExtensions.Add(new ReactivePropertySlim<string>(""));

        internal void AddDeleteTexts() => DeleteTexts.Add(new ReplacePattern("", ""));

        internal void AddReplaceTexts() => ReplaceTexts.Add(new ReplacePattern("", ""));

        /// <summary>
        /// ファイルから設定ファイルをデシリアライズ
        /// </summary>
        public static SettingAppModel Deserialize()
        {
            var json = File.ReadAllText(SettingFilePath);
            var mPack = MessagePack.MessagePackSerializer.ConvertFromJson(json);
            return MessagePackSerializer.Deserialize<SettingAppModel>(mPack);
        }

        /// <summary>
        /// ファイルに設定ファイルをシリアライズ
        /// </summary>
        public void Serialize()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingFilePath));
            var json = MessagePackSerializer.SerializeToJson(this);
            File.WriteAllText(SettingFilePath, json);
        }
    }
}