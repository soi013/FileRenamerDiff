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
    public class SettingAppModel : NotificationObject
    {
        private static readonly string myDocPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public ReactivePropertySlim<string> SourceFilePath { get; set; } = new ReactivePropertySlim<string>(myDocPath);

        public ObservableCollection<ReactivePropertySlim<string>> IgnoreExtensions { get; set; } = new[]
        {
            "pdb", "db", "xfr","ini","",
            "", "", "", "", "",
        }
        .Select(x => new ReactivePropertySlim<string>(x))
        .ToObservableCollection();

        public Regex CreateIgnoreExtensionsRegex()
        {
            IEnumerable<string> ignoreExts = IgnoreExtensions.Select(x => x.Value).Where(x => !String.IsNullOrWhiteSpace(x));
            var ignorePattern = string.Join('|', ignoreExts);
            return new Regex(ignorePattern, RegexOptions.Compiled);
        }


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

        static SettingAppModel()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                ReactivePropertyResolver.Instance,
                MessagePack.Resolvers.ContractlessStandardResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
            );
            MessagePackSerializer.DefaultOptions = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        private static readonly string settingFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            + $@"\{nameof(FileRenamerDiff)}\{nameof(SettingAppModel)}.json";

        public static SettingAppModel Deserialize()
        {
            var json = File.ReadAllText(settingFilePath);
            var mPack = MessagePack.MessagePackSerializer.ConvertFromJson(json);
            return MessagePackSerializer.Deserialize<SettingAppModel>(mPack);
        }

        public void Serialize()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingFilePath));
            var json = MessagePackSerializer.SerializeToJson(this);
            File.WriteAllText(settingFilePath, json);
        }
    }
}