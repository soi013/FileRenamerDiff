using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Livet;
using Microsoft.VisualBasic;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace FileRenamerDiff.Models
{
    public class Model : NotificationObject
    {
        public static Model Instance { get; } = new Model();

        private IReadOnlyList<FilePathModel> _SourceFilePathVMs = new[] { new FilePathModel(@"c:\abc\my_file.txt") };
        public IReadOnlyList<FilePathModel> SourceFilePathVMs
        {
            get => _SourceFilePathVMs;
            set => RaisePropertyChangedIfSet(ref _SourceFilePathVMs, value);
        }

        private SettingAppModel _Setting;
        public SettingAppModel Setting
        {
            get => _Setting;
            set => RaisePropertyChangedIfSet(ref _Setting, value);
        }

        public IReadOnlyReactiveProperty<int> CountReplaced => countReplaced;
        private ReactivePropertySlim<int> countReplaced = new ReactivePropertySlim<int>(0);

        public ReactivePropertySlim<bool> IsIdle { get; } = new ReactivePropertySlim<bool>(false);

        private Model()
        {
            LoadSetting();
        }

        internal void Initialize()
        {
            IsIdle.Value = true;
        }

        public async Task LoadSourceFiles()
        {
            this.IsIdle.Value = false;
            string sourceFilePath = Setting.SourceFilePath.Value;
            if (!Directory.Exists(sourceFilePath))
                return;

            await Task.Run(() =>
            {
                var regex = Setting.CreateIgnoreExtensionsRegex();

                this.SourceFilePathVMs = Directory
                    .EnumerateFileSystemEntries(sourceFilePath, "*.*", SearchOption.AllDirectories)
                    .Where(x => !regex.IsMatch(Path.GetExtension(x)))
                    //Rename時にエラーしないように、フォルダ階層が深い側から変更されるように並び替え
                    .OrderByDescending(x => x)
                    .Select(x => new FilePathModel(x))
                    .ToArray();
            })
            .ConfigureAwait(false);

            this.countReplaced.Value = 0;
            this.IsIdle.Value = true;
        }

        internal void ResetSetting()
        {
            this.Setting = new SettingAppModel();
        }

        private void LoadSetting()
        {
            try
            {
                Setting = SettingAppModel.Deserialize();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error {ex.Message}");
                Setting = new SettingAppModel();
            }
        }

        internal void SaveSetting()
        {
            try
            {
                Setting.Serialize();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error {ex.Message}");
            }
        }

        internal async Task Replace()
        {
            this.IsIdle.Value = false;
            await Task.Run(() =>
            {
                var regexes = CreateRegexes();
                Parallel.ForEach(SourceFilePathVMs,
                    x => x.Replace(regexes));
            })
            .ConfigureAwait(false);

            this.countReplaced.Value = SourceFilePathVMs.Count(x => x.IsReplaced);
            this.IsIdle.Value = true;
        }


        internal List<ReplaceRegex> CreateRegexes()
        {
            var deletePatterns = Setting.DeleteTexts
                .Select(x =>
                    x.AsExpression
                        ? x.Pattern
                        : Regex.Escape(x.Pattern));

            var deleteInput = String.Join('|', deletePatterns);
            var totalReplaceTexts = Setting.ReplaceTexts.ToList();
            totalReplaceTexts.Insert(0, new ReplacePattern(deleteInput, string.Empty, true));

            return totalReplaceTexts
                .Where(a => !String.IsNullOrWhiteSpace(a.Pattern))
               .Select(a => new ReplaceRegex(a))
               .ToList();
        }

        internal async Task RenameExcute()
        {
            IsIdle.Value = false;
            await Task.Run(() =>
            {
                try
                {
                    foreach (var replacePath in SourceFilePathVMs.Where(x => x.IsReplaced))
                        replacePath.Rename();
                }
                catch (FileNotFoundException fex)
                {
                    Trace.WriteLine($"Warn Fail to Rename ex:{fex.Message}");
                }
            })
            .ConfigureAwait(false);

            await LoadSourceFiles().ConfigureAwait(false);
            IsIdle.Value = true;
        }
    }
}