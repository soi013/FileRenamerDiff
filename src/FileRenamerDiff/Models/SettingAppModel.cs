using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Anotar.Serilog;
using Livet;
using MessagePack;
using MessagePack.ReactivePropertyExtension;
using MessagePack.Resolvers;
using Reactive.Bindings;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// アプリケーション設定クラス
    /// </summary>
    public class SettingAppModel : NotificationObject
    {
        internal static readonly string SettingFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            + $@"\{nameof(FileRenamerDiff)}\{nameof(SettingAppModel)}.json";

        private static readonly string myDocPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>
        /// リネームファイルを検索するターゲットパス
        /// </summary>
        public ReactivePropertySlim<string> SearchFilePath =>
            searchFilePath ??= new ReactivePropertySlim<string>(myDocPath);
        private ReactivePropertySlim<string> searchFilePath;

        /// <summary>
        /// 検索時に無視される拡張子コレクション
        /// </summary>
        public ObservableCollection<ReactivePropertySlim<string>> IgnoreExtensions =>
            ignoreExtensions ??= new[]
            {
                "pdb", "db", "xfr","ini","",
                "", "", "", "", "",
            }
            .Select(x => new ReactivePropertySlim<string>(x))
            .ToObservableCollection();
        private ObservableCollection<ReactivePropertySlim<string>> ignoreExtensions;

        /// <summary>
        /// 検索時に無視される拡張子判定Regexの生成
        /// </summary>
        public Regex CreateIgnoreExtensionsRegex()
        {
            IEnumerable<string> ignoreExts = IgnoreExtensions.Select(x => x.Value).Where(x => !String.IsNullOrWhiteSpace(x));
            var ignorePattern = string.Join('|', ignoreExts);
            return new Regex(ignorePattern, RegexOptions.Compiled);
        }

        /// <summary>
        /// 削除文字列パターン
        /// </summary>
        public ObservableCollection<ReplacePattern> DeleteTexts =>
            deleteTexts ??= new[]
            {
                 " - コピー",
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
                new ReplacePattern("　", " "),
                new ReplacePattern("   ", " "),
                new ReplacePattern("  ", " "),
                new ReplacePattern("", ""),
                new ReplacePattern("", ""),
                new ReplacePattern("", ""),
                new ReplacePattern("", ""),
                new ReplacePattern("", ""),
            };
        private ObservableCollection<ReplacePattern> replaceTexts;

        /// <summary>
        /// ファイル探索時にサブディレクトリを探索するか
        /// </summary>
        public ReactivePropertySlim<bool> IsSearchSubDirectories =>
           isSearchSubDirectories ??= new ReactivePropertySlim<bool>(true);
        private ReactivePropertySlim<bool> isSearchSubDirectories;

        /// <summary>
        /// ファイル探索時にディレクトリ自身もリストに含めるか
        /// </summary>
        public ReactivePropertySlim<bool> IsIgnoreDirectory =>
            isIgnoreDirectory ??= new ReactivePropertySlim<bool>(true);
        private ReactivePropertySlim<bool> isIgnoreDirectory;

        /// <summary>
        /// アプリケーションの表示言語
        /// </summary>
        public ReactivePropertySlim<string> AppLanguageCode =>
            appLanguage ??= new ReactivePropertySlim<string>("");
        private ReactivePropertySlim<string> appLanguage;

        static SettingAppModel()
        {
            //ReactivePropertyをシリアライズ可能にするため、アプリケーション全体で固定のMessagePackResolverを設定
            var resolver = CompositeResolver.Create(
                ReactivePropertyResolver.Instance,
                ContractlessStandardResolverAllowPrivate.Instance
            );
            MessagePackSerializer.DefaultOptions = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);
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