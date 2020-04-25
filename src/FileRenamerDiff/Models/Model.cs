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
    /// <summary>
    /// アプリケーション全体シングルトンモデル
    /// </summary>
    public class Model : NotificationObject
    {
        /// <summary>
        /// シングルトンなインスタンスを返す
        /// </summary>
        public static Model Instance { get; } = new Model();

        private IReadOnlyList<FileElementModel> _FileElementModels = new[] { new FileElementModel(@"c:\abc\my_file.txt") };
        /// <summary>
        /// リネーム対象ファイル情報のコレクション
        /// </summary>
        public IReadOnlyList<FileElementModel> FileElementModels
        {
            get => _FileElementModels;
            set => RaisePropertyChangedIfSet(ref _FileElementModels, value);
        }

        private SettingAppModel _Setting;
        /// <summary>
        /// アプリケーション設定
        /// </summary>
        public SettingAppModel Setting
        {
            get => _Setting;
            set => RaisePropertyChangedIfSet(ref _Setting, value);
        }

        /// <summary>
        /// リネーム前後での変更があったファイル数
        /// </summary>
        public IReadOnlyReactiveProperty<int> CountReplaced => countReplaced;
        private ReactivePropertySlim<int> countReplaced = new ReactivePropertySlim<int>(0);

        /// <summary>
        /// アプリケーションが待機状態か
        /// </summary>
        public ReactivePropertySlim<bool> IsIdle { get; } = new ReactivePropertySlim<bool>(false);

        private Model()
        {
            LoadSetting();
        }

        /// <summary>
        /// アプリケーション起動時処理
        /// </summary>
        internal void Initialize()
        {
            IsIdle.Value = true;
        }

        /// <summary>
        /// ターゲットパスをもとにリネーム対象ファイルを検索して、ファイル情報コレクションに読み込む
        /// </summary>
        public async Task LoadFileElements()
        {
            this.IsIdle.Value = false;
            string sourceFilePath = Setting.SearchFilePath.Value;
            if (!Directory.Exists(sourceFilePath))
                return;

            await Task.Run(() =>
            {
                var regex = Setting.CreateIgnoreExtensionsRegex();

                this.FileElementModels = Directory
                    .EnumerateFileSystemEntries(sourceFilePath, "*.*", SearchOption.AllDirectories)
                    .Where(x => !regex.IsMatch(Path.GetExtension(x)))
                    //Rename時にエラーしないように、フォルダ階層が深い側から変更されるように並び替え
                    .OrderByDescending(x => x)
                    .Select(x => new FileElementModel(x))
                    .ToArray();
            })
            .ConfigureAwait(false);

            this.countReplaced.Value = 0;
            this.IsIdle.Value = true;
        }

        /// <summary>
        /// 設定の初期化
        /// </summary>
        internal void ResetSetting() => this.Setting = new SettingAppModel();

        /// <summary>
        /// 設定読込
        /// </summary>
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

        /// <summary>
        /// 設定保存
        /// </summary>
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

        /// <summary>
        /// 置換実行（ファイルにはまだ保存しない）
        /// </summary>
        internal async Task Replace()
        {
            this.IsIdle.Value = false;
            await Task.Run(() =>
            {
                var regexes = CreateRegexes();
                Parallel.ForEach(FileElementModels,
                    x => x.Replace(regexes));
            })
            .ConfigureAwait(false);

            this.countReplaced.Value = FileElementModels.Count(x => x.IsReplaced);
            this.IsIdle.Value = true;
        }


        /// <summary>
        /// 設定をもとに置換パターンコレクションを作成する
        /// </summary>
        internal List<ReplaceRegex> CreateRegexes()
        {
            //削除パターンは置換後文字列がstring.Emptyなので、１つのReplacePatternにまとめる
            string deleteInput = Setting.DeleteTexts
                .Select(x =>
                    x.AsExpression
                        ? x.TargetPattern
                        : Regex.Escape(x.TargetPattern))
                .ConcatenateString('|');

            var totalReplaceTexts = Setting.ReplaceTexts.ToList();
            totalReplaceTexts.Insert(0, new ReplacePattern(deleteInput, string.Empty, true));

            return totalReplaceTexts
                .Where(a => !String.IsNullOrWhiteSpace(a.TargetPattern))
               .Select(a => new ReplaceRegex(a))
               .ToList();
        }

        /// <summary>
        /// 置換後ファイル名を保存する
        /// </summary>
        internal async Task RenameExcute()
        {
            IsIdle.Value = false;
            await Task.Run(() =>
            {
                try
                {
                    foreach (var replacePath in FileElementModels.Where(x => x.IsReplaced))
                        replacePath.Rename();
                }
                catch (FileNotFoundException fex)
                {
                    Trace.WriteLine($"Warn Fail to Rename ex:{fex.Message}");
                }
            })
            .ConfigureAwait(false);

            await LoadFileElements().ConfigureAwait(false);
            IsIdle.Value = true;
        }
    }
}