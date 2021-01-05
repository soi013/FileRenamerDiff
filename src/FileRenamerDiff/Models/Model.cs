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
using System.Threading;
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
using Reactive.Bindings.Notifiers;
using Anotar.Serilog;

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
        public static Model Instance { get; } = new();

        /// <summary>
        /// アプリケーションが待機状態か（変更用）
        /// </summary>
        readonly ReactivePropertySlim<bool> isIdle = new(false);

        /// <summary>
        /// アプリケーションが待機状態か（外部購読用）
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsIdle => isIdle;

        /// <summary>
        /// アプリケーションが待機状態か（UI購読用）
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsIdleUI => isIdle.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();

        private IReadOnlyList<FileElementModel> _FileElementModels = Array.Empty<FileElementModel>();
        /// <summary>
        /// リネーム対象ファイル情報のコレクション
        /// </summary>
        public IReadOnlyList<FileElementModel> FileElementModels
        {
            get => _FileElementModels;
            set => RaisePropertyChangedIfSet(ref _FileElementModels, value);
        }

        private SettingAppModel _Setting = new();
        /// <summary>
        /// アプリケーション設定
        /// </summary>
        public SettingAppModel Setting
        {
            get => _Setting;
            set => RaisePropertyChangedIfSet(ref _Setting, value);
        }

        /// <summary>
        /// 前回保存設定ファイルパス
        /// </summary>
        public ReactivePropertySlim<string> PreviousSettingFilePath { get; } = new(SettingAppModel.DefaultFilePath);

        /// <summary>
        /// リネーム前後での変更があったファイル数
        /// </summary>
        public IReadOnlyReactiveProperty<int> CountReplaced => countReplaced;
        private readonly ReactivePropertySlim<int> countReplaced = new(0);


        /// <summary>
        /// ファイルパスの衝突しているファイル数
        /// </summary>
        public IReadOnlyReactiveProperty<int> CountConflicted => countConflicted;
        private readonly ReactivePropertySlim<int> countConflicted = new(0);

        /// <summary>
        /// アプリケーション内メッセージ読み取り専用
        /// </summary>
        public IReadOnlyReactiveProperty<AppMessage> MessageEventStream => MessageEvent;
        /// <summary>
        /// アプリケーション内メッセージ変更用
        /// </summary>
        internal ReactivePropertySlim<AppMessage> MessageEvent { get; } = new();

        /// <summary>
        /// 処理状態メッセージ通知
        /// </summary>
        readonly ScheduledNotifier<ProgressInfo> progressNotifier = new();

        /// <summary>
        /// 現在の処理状態メッセージ
        /// </summary>
        public IReadOnlyReactiveProperty<ProgressInfo?> CurrentProgessInfo { get; }

        /// <summary>
        /// 現在の処理のキャンセルトークン
        /// </summary>
        public CancellationTokenSource? CancelWork { get; private set; }

        /// <summary>
        /// ユーザー確認デリゲート
        /// </summary>
        public Func<Task<bool>> ConfirmUser { get; set; } = () => Task.FromResult(true);

        private Model()
        {
            CurrentProgessInfo = progressNotifier
                .ToReadOnlyReactivePropertySlim();

            LoadSettingFile(SettingAppModel.DefaultFilePath);
            //設定に応じてアプリケーションの言語を変更する
            UpdateLanguage(Setting.AppLanguageCode);
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
            string sourceFilePath = Setting.SearchFilePath;
            using (CancelWork = new CancellationTokenSource())
            {
                try
                {
                    await Task.Run(() =>
                        this.FileElementModels =
                            LoadFileElementsCore(sourceFilePath, Setting, progressNotifier, CancelWork.Token))
                        .ConfigureAwait(false);


                    if (!FileElementModels.Any())
                        MessageEvent.Value = new AppMessage(AppMessageLevel.Alert, "NOT FOUND", Resources.Alert_NotFoundFileBody);
                }
                catch (OperationCanceledException)
                {
                    progressNotifier.Report(new(0, "canceled"));
                    this.FileElementModels = Array.Empty<FileElementModel>();
                }
            }

            this.countReplaced.Value = 0;
            this.countConflicted.Value = 0;
            this.isIdle.Value = true;
            LogTo.Debug("File Load Ended");
        }

        private static FileElementModel[] LoadFileElementsCore(string sourceFilePath, SettingAppModel setting, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(sourceFilePath) || setting.IsNoFileRenameTarget())
                return Array.Empty<FileElementModel>();

            var ignoreRegex = setting.CreateIgnoreExtensionsRegex();

            var option = new EnumerationOptions()
            {
                //読み取り権限のない場合は無視
                IgnoreInaccessible = true,
                RecurseSubdirectories = setting.IsSearchSubDirectories,
                //ディレクトリとファイルがターゲットとなるかで、スキップする属性を指定。後でフィルタするよりも効率がいい。
                //ただし、ディレクトリ自体がターゲット出ない場合もサブディレクトリを探索するなら、スキップできない
                AttributesToSkip = setting.GetSkipAttribute()
            };

            IEnumerable<string> fileEnums = Directory.EnumerateFileSystemEntries(sourceFilePath, "*", option);

            var loadedFileList = fileEnums
                //無視する拡張子が無い、または一致しないだけ残す
                .Where(x => ignoreRegex?.IsMatch(AppExtention.GetExtentionCoreFromPath(x)) != true)
                .Do((x, i) =>
                    {
                        //i%256と同じ。全部をレポート出力する必要はないので、何回かに1回に減らす
                        if ((i & 0xFF) != 0)
                            return;
                        progress?.Report(new(i, $"File Loaded {x}"));
                        cancellationToken.ThrowIfCancellationRequested();
                    })
                .ToList();

            progress?.Report(new(loadedFileList.Count, "Files were Loaded. Creating FileList"));
            //Rename時にエラーしないように、フォルダ階層が深い側から変更されるように並び替え
            loadedFileList.Sort();
            loadedFileList.Reverse();

            cancellationToken.ThrowIfCancellationRequested();

            return loadedFileList
                .Select(x => new FileInfo(x))
                //設定に応じて、ディレクトリ・ファイル・隠しファイルの表示状態を変更
                .Where(x => setting.IsTargetFile(x))
                .Select(x => new FileElementModel(x))
                .ToArray();
        }

        internal static void UpdateLanguage(string langCode)
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
        internal void LoadSettingFile(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                Setting = SettingAppModel.Deserialize(filePath);
                PreviousSettingFilePath.Value = filePath;
            }
            catch (Exception ex)
            {
                LogTo.Warning(ex, "Can not Load Setting {@SettingFilePath}", filePath);
            }
        }

        /// <summary>
        /// 設定保存
        /// </summary>
        internal void SaveSettingFile(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                Setting.Serialize(filePath);
                PreviousSettingFilePath.Value = filePath;
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
                .Select(a => a.ToReplaceRegex())
                .WhereNotNull()
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
                    progressNotifier.Report(new(0, "canceled"));
                }
            }

            await LoadFileElements().ConfigureAwait(false);

            isIdle.Value = true;
            LogTo.Information("Renamed File Save Ended");
        }

        private void RenameExcuteCore(IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
        {
            foreach (var (replaceElement, index) in FileElementModels.Where(x => x.IsReplaced).WithIndex())
            {
                replaceElement.Rename();

                if (index % 16 == 0)
                {
                    progress.Report(new(index, $"File Renamed {replaceElement.OutputFilePath}"));
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            if (Setting.IsCreateRenameLog)
                SaveReplaceLog();

            string failElementsText = FileElementModels
                .Where(x => x.State == RenameState.FailedToRename)
                .Select(x => $"{x.InputFileName} -> {x.OutputFileName} ({x.DirectoryPath})")
                .ConcatenateString(Environment.NewLine);

            if (String.IsNullOrWhiteSpace(failElementsText))
                return;

            MessageEvent.Value = new AppMessage(
                AppMessageLevel.Error,
                head: Resources.Alert_FailSaveRename,
                body: failElementsText);
        }

        /// <summary>
        /// リネーム前後の履歴ファイルのヘッダ文字列リスト
        /// </summary>
        static readonly string renameLogHeadderTexts = new[] { "State", "InputFilePath", "OutputFilePath" }.ConcatenateString(',');

        /// <summary>
        /// リネーム前後の履歴の保存
        /// </summary>
        private void SaveReplaceLog()
        {
            if (CountReplaced.Value <= 0)
                return;
            string logFilePath = CreateLogFilePath();

            using var sw = new StreamWriter(logFilePath);
            sw.WriteLine(renameLogHeadderTexts);

            foreach (var fileElem in FileElementModels.Where(x => x.State != RenameState.None))
            {
                var lineText = new[]
                {
                    fileElem.State.ToString(),
                    //ファイルパスにはカンマが含まれる可能性があるので、ダブルクオーテーションで囲う
                    '"' + fileElem.PreviousInputFilePath + '"',
                    '"' + fileElem.OutputFilePath + '"',
                }
                .ConcatenateString(',');

                sw.WriteLine(lineText);
            }
        }

        private string CreateLogFilePath()
        {
            string logFilePath = Path.Combine(Setting.SearchFilePath, $"RenameLog {DateTime.Now:yyyy-MM-dd HH-mm-ss}.csv");
            while (File.Exists(logFilePath))
            {
                logFilePath = AppExtention.GetFilePathWithoutExtension(logFilePath) + "_.csv";
            }
            return logFilePath;
        }

        internal async Task ExcuteAfterConfirm(Action actionIfConfirmed)
        {
            bool isConfirmed = await ConfirmUser();
            if (isConfirmed)
                actionIfConfirmed();
        }
    }
}