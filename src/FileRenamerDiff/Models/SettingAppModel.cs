using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Livet;
using MessagePack;
using MessagePack.ReactivePropertyExtension;
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
        public ReactivePropertySlim<string> SearchFilePath { get; set; } = new ReactivePropertySlim<string>(myDocPath);

        /// <summary>
        /// 検索時に無視される拡張子コレクション
        /// </summary>
        public ObservableCollection<ReactivePropertySlim<string>> IgnoreExtensions { get; set; } = new[]
        {
            "pdb", "db", "xfr","ini","",
            "", "", "", "", "",
        }
        .Select(x => new ReactivePropertySlim<string>(x))
        .ToObservableCollection();

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
        public ObservableCollection<ReplacePattern> DeleteTexts { get; set; } = new[]
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

        /// <summary>
        /// 置換文字列パターン
        /// </summary>
        public ObservableCollection<ReplacePattern> ReplaceTexts = new ObservableCollection<ReplacePattern>
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

        /// <summary>
        /// ファイル探索時にサブディレクトリを探索するか
        /// </summary>
        public ReactivePropertySlim<bool> IsSearchSubDirectories { get; set; } = new ReactivePropertySlim<bool>(true);

        static SettingAppModel()
        {
            //ReactivePropertyをシリアライズ可能にするため、アプリケーション全体で固定のMessagePackResolverを設定

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                ReactivePropertyResolver.Instance,
                MessagePack.Resolvers.ContractlessStandardResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
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