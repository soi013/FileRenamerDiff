using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;

using Serilog.Events;
using Anotar.Serilog;
using Livet;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

using FileRenamerDiff.Properties;

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

        /// <summary>
        /// アプリケーションが待機状態か（変更用）
        /// </summary>
        private ReactivePropertySlim<bool> isIdle { get; } = new ReactivePropertySlim<bool>(false);

        /// <summary>
        /// アプリケーションが待機状態か（外部購読用）
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsIdle => isIdle;

        /// <summary>
        /// アプリケーションが待機状態か（UI購読用）
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsIdleUI => isIdle.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();

        private IReadOnlyList<FileElementModel> _FileElementModels = new FileElementModel[]
        {
            //new FileElementModel(@"c:\abc\sample.txt"),
            //new FileElementModel(@"c:\def\sample(1).txt"),
        };

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
        private readonly ReactivePropertySlim<int> countReplaced = new ReactivePropertySlim<int>(0);


        /// <summary>
        /// ファイルパスの衝突しているファイル数
        /// </summary>
        public IReadOnlyReactiveProperty<int> CountConflicted => countConflicted;
        private readonly ReactivePropertySlim<int> countConflicted = new ReactivePropertySlim<int>(0);

        /// <summary>
        /// アプリケーション内メッセージ読み取り専用
        /// </summary>
        public IReadOnlyReactiveProperty<AppMessage> MessageEventStream => MessageEvent;
        /// <summary>
        /// アプリケーション内メッセージ変更用
        /// </summary>
        internal ReactivePropertySlim<AppMessage> MessageEvent { get; } = new ReactivePropertySlim<AppMessage>();

        /// <summary>
        /// 処理状態メッセージ通知
        /// </summary>
        private ScheduledNotifier<ProgressInfo> progressNotifier = new ScheduledNotifier<ProgressInfo>();

        /// <summary>
        /// 現在の処理状態メッセージ
        /// </summary>
        public IReadOnlyReactiveProperty<ProgressInfo> CurrentProgessInfo { get; }

        /// <summary>
        /// 現在の処理のキャンセルトークン
        /// </summary>
        public CancellationTokenSource CancelWork { get; private set; }

        /// <summary>
        /// ユーザー確認デリゲート
        /// </summary>
        public Func<Task<bool>> ConfirmUser { get; set; }

        private Model()
        {
            CurrentProgessInfo = progressNotifier
                .ToReadOnlyReactivePropertySlim();

            LoadSetting();
            //設定に応じてアプリケーションの言語を変更する
            UpdateLanguage(Setting.AppLanguageCode.Value);
        }

        /// <summary>
        /// アプリケーション起動時処理
        /// </summary>
        internal void Initialize()
        {
            isIdle.Value = true;
        }

        /// <summary>
        /// ターゲットパスをもとにリネーム対象ファイルを検索して、ファイル情報コレクションに読み込む
        /// </summary>
        public async Task LoadFileElements()
        {
            LogTo.Debug("File Load Start");
            this.isIdle.Value = false;
            string sourceFilePath = Setting?.SearchFilePath.Value;
            if (Directory.Exists(sourceFilePath))
            {
                using (CancelWork = new CancellationTokenSource())
                {
                    try
                    {
                        await Task.Run(() =>
                            this.FileElementModels =
                                LoadFileElementsCore(sourceFilePath, Setting, progressNotifier, CancelWork.Token))
                            .ConfigureAwait(false); ;
                    }
                    catch (OperationCanceledException)
                    {
                        progressNotifier.Report(new ProgressInfo(0, "canceled"));
                        this.FileElementModels = Array.Empty<FileElementModel>();
                    }
                }
            }

            this.countReplaced.Value = 0;
            this.countConflicted.Value = 0;
            this.isIdle.Value = true;
            LogTo.Debug("File Load Ended");
        }

        private static FileElementModel[] LoadFileElementsCore(string sourceFilePath, SettingAppModel setting, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
        {
            var regex = setting.CreateIgnoreExtensionsRegex();

            var option = new EnumerationOptions()
            {
                //読み取り権限のない場合は無視
                IgnoreInaccessible = true,
                RecurseSubdirectories = setting.IsSearchSubDirectories.Value,
            };

            IEnumerable<string> fileEnums = setting.IsIgnoreDirectory.Value
                ? Directory.EnumerateFileSystemEntries(sourceFilePath, "*", option)
                : Directory.EnumerateFiles(sourceFilePath, "*", option);

            var loadedFileList = fileEnums
                .Where(x => !regex.IsMatch(Path.GetExtension(x)))
                .Do((x, i) =>
                    {
                        if (i % 16 != 0)
                            return;
                        progress?.Report(new ProgressInfo(i, $"File Loaded {x}"));
                        cancellationToken.ThrowIfCancellationRequested();
                    })
                .ToList();

            progress?.Report(new ProgressInfo(0, "Files were Loaded. Sorting Files"));
            //Rename時にエラーしないように、フォルダ階層が深い側から変更されるように並び替え
            loadedFileList.Sort();
            cancellationToken.ThrowIfCancellationRequested();
            loadedFileList.Reverse();

            progress?.Report(new ProgressInfo(0, "Files were Sorted. Creating FileList"));
            cancellationToken.ThrowIfCancellationRequested();

            return loadedFileList
                .Select(x => new FileElementModel(x))
                .ToArray();
        }

        internal void UpdateLanguage(string langCode)
        {
            if (String.IsNullOrWhiteSpace(langCode))
                return;

            Properties.Resources.Culture = CultureInfo.GetCultureInfo(langCode);
        }

        /// <summary>
        /// 設定の初期化
        /// </summary>
        internal void ResetSetting()
        {
            this.Setting = new SettingAppModel();
            LogTo.Information("Reset Setting");
            MessageEvent.Value = new AppMessage(AppMessageLevel.Info, head: Resources.Info_SettingsReset);
        }

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
                LogTo.Warning(ex, "Can not Load Setting {@SettingFilePath}", SettingAppModel.SettingFilePath);
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
                LogTo.Error(ex, "Fail to Save Setting");
                MessageEvent.Value = new AppMessage(AppMessageLevel.Error, head: Resources.Alert_FailSaveSetting, body: ex.Message);
            }
        }

        /// <summary>
        /// 置換実行（ファイルにはまだ保存しない）
        /// </summary>
        internal async Task Replace()
        {
            LogTo.Information("Replace Start");
            this.isIdle.Value = false;
            await Task.Run(() =>
            {
                var regexes = CreateRegexes();
                Parallel.ForEach(FileElementModels,
                    x => x.Replace(regexes));

                this.countReplaced.Value = FileElementModels.Count(x => x.IsReplaced);

                UpdateFilePathConflict();
                this.countConflicted.Value = FileElementModels.Count(x => x.IsConflicted);
            });

            if (CountConflicted.Value >= 1)
            {
                LogTo.Warning("Some fileNames are DUPLICATED {@count}", CountConflicted.Value);
                MessageEvent.Value = new AppMessage(
                    AppMessageLevel.Alert,
                    head: Resources.Alert_FileNamesDuplicated,
                    body: FileElementModels
                        .Where(x => x.IsConflicted)
                        .Select(x => x.OutputFileName)
                        .ConcatenateString(Environment.NewLine));
            }

            this.isIdle.Value = true;
            LogTo.Information("Replace Ended");
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
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .Distinct()
                .ConcatenateString('|');

            var totalReplaceTexts = Setting.ReplaceTexts.ToList();
            totalReplaceTexts.Insert(0, new ReplacePattern(deleteInput, string.Empty, true));

            return totalReplaceTexts
                .Where(a => !String.IsNullOrWhiteSpace(a.TargetPattern))
                .Distinct(a => a.TargetPattern)
                .Select(a => new ReplaceRegex(a))
                .ToList();
        }

        private void UpdateFilePathConflict()
        {
            var lowerAllPaths = FileElementModels
                .SelectMany(x =>
                //大文字小文字を区別せず一致していたら、Inputのみ返す
                    (String.Compare(x.InputFilePath, x.OutputFilePath, true) == 0)
                     ? new[] { x.InputFilePath }
                     : new[] { x.InputFilePath, x.OutputFilePath })
                //Windowsの場合、ファイルパスの衝突は大文字小文字を区別しないので、小文字にしておく
                .Select(x => x.ToLower())
                .ToArray();

            foreach (var fileElement in FileElementModels)
            {
                //Windowsの場合、ファイルパスの衝突は大文字小文字を区別しないので、小文字にしておく
                string lowPath = fileElement.OutputFilePath.ToLower();
                int matchPathCount = lowerAllPaths
                    .Where(x => x == lowPath)
                    .Count();
                //もともとのファイルパスがあるので、2以上のときは衝突していると判定
                fileElement.IsConflicted = matchPathCount >= 2;
            }
        }

        /// <summary>
        /// リネームを実行（ストレージに保存される）
        /// </summary>
        internal async Task RenameExcute()
        {
            LogTo.Information("Renamed File Save Start");
            isIdle.Value = false;

            using (CancelWork = new CancellationTokenSource())
            {
                try
                {
                    await Task.Run(() => RenameExcuteCore(progressNotifier, CancelWork.Token)).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    progressNotifier.Report(new ProgressInfo(0, "canceled"));
                }
            }

            await LoadFileElements().ConfigureAwait(false);

            isIdle.Value = true;
            LogTo.Information("Renamed File Save Ended");
        }

        private void RenameExcuteCore(IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
        {
            var failFileElements = new List<FileElementModel>();

            foreach (var (replaceElement, index) in FileElementModels.Where(x => x.IsReplaced).WithIndex())
            {
                try
                {
                    replaceElement.Rename();
                }
                catch (Exception ex)
                {
                    LogTo.Warning(ex, "Fail to Rename {@fileElement}", replaceElement);
                    failFileElements.Add(replaceElement);
                }

                if (index % 16 == 0)
                {
                    progress.Report(new ProgressInfo(index, $"File Renamed {replaceElement.OutputFilePath}"));
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            if (!failFileElements.Any())
                return;

            MessageEvent.Value = new AppMessage(
                AppMessageLevel.Error,
                head: Resources.Alert_FailSaveRename,
                body: failFileElements
                    .Select(x => $"{x.InputFilePath} -> {x.OutputFilePath}")
                    .ConcatenateString(Environment.NewLine));
        }

        internal async Task ExcuteAfterConfirm(Action actionIfConfirmed)
        {
            bool isConfirmed = await ConfirmUser();
            if (isConfirmed)
                actionIfConfirmed();
        }
    }
}