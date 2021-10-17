using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Anotar.Serilog;

using FileRenamerDiff.Models;
using FileRenamerDiff.Properties;

using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;

using Microsoft.Extensions.DependencyInjection;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace FileRenamerDiff.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        private readonly MainModel mainModel;
        private readonly IScheduler uiScheduler;

        /// <summary>
        /// アプリケーションタイトル文字列
        /// </summary>
        public ReadOnlyReactivePropertySlim<string> WindowTitle { get; }

        /// <summary>
        /// アプリケーションが待機状態か
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsIdle => mainModel.IsIdleUI;

        /// <summary>
        /// ダイアログ表示VM
        /// </summary>
        public ReactivePropertySlim<ViewModel?> DialogContentVM { get; } = new();
        /// <summary>
        /// ダイアログが表示されているか
        /// </summary>
        public ReactivePropertySlim<bool> IsDialogOpen { get; } = new(false);
        /// <summary>
        /// ダイアログの外側をクリックした際に閉じられるか
        /// </summary>
        public ReactivePropertySlim<bool> CloseOnClickAwayDialog { get; } = new(true);

        /// <summary>
        /// ファイルVMコレクションを含んだDataGrid用VM
        /// </summary>
        public FileElementsGridViewModel GridVM { get; }

        /// <summary>
        /// フォルダ選択メッセージでのフォルダ読込コマンド
        /// </summary>
        public AsyncReactiveCommand<FolderSelectionMessage> LoadFilesFromDialogCommand { get; }
        /// <summary>
        /// パス指定でのフォルダ読込コマンド
        /// </summary>
        public AsyncReactiveCommand<IReadOnlyList<string>> LoadFilesFromNewPathCommand { get; }
        /// <summary>
        /// フォルダ読み込みコマンド
        /// </summary>
        public AsyncReactiveCommand LoadFilesFromCurrentPathCommand { get; }

        public ReactiveCommand<OpeningFileSelectionMessage> AddFilesFromDialogCommand { get; }

        /// <summary>
        /// 置換実行コマンド
        /// </summary>
        public AsyncReactiveCommand ReplaceCommand { get; }

        /// <summary>
        /// 置換後ファイル名保存コマンド
        /// </summary>
        public AsyncReactiveCommand RenameExecuteCommand { get; }

        /// <summary>
        /// アプリケーション情報表示コマンド
        /// </summary>
        public ReactiveCommand ShowInformationPageCommand { get; }
        /// <summary>
        /// ヘルプ表示コマンド
        /// </summary>
        public AsyncReactiveCommand ShowHelpPageCommand { get; }

        /// <summary>
        /// 設定情報ViewModel
        /// </summary>
        public ReadOnlyReactivePropertySlim<SettingAppViewModel> SettingVM { get; }

        /// <summary>
        /// アプリケーション内メッセージをまとめる時間長
        /// </summary>
        internal readonly static TimeSpan TimeSpanMessageBuffer = TimeSpan.FromMilliseconds(100);

        public MainWindowViewModel() : this(App.Services.GetService<MainModel>()!) { }
        public MainWindowViewModel(MainModel mainModel)
        {
            this.mainModel = mainModel;
            this.uiScheduler = mainModel.UIScheduler;
            this.GridVM = new(mainModel);
            var concatedFilePaths = mainModel.FileElementModels.CollectionChangedAsObservable()
                            .Select(_ => mainModel.FileElementModels.Count > 0 ? mainModel.Setting.ConcatedSearchFilePaths : string.Empty)
                            .ObserveOn(uiScheduler)
                            //起動時にはCollectionChangedが動かないので、ダミーの初期値を入れておく
                            .ToReadOnlyReactivePropertySlim(string.Empty);

            this.WindowTitle = concatedFilePaths
                .Select(x => $"FILE RENAMER DIFF | {x}")
                .ToReadOnlyReactivePropertySlim<string>();

            this.ReplaceCommand = new[]
                {
                    mainModel.FileElementModels.ObserveIsAny(),
                    IsIdle
                }
                .CombineLatestValuesAreAllTrue()
                .ObserveOn(uiScheduler)
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => mainModel.Replace());

            this.RenameExecuteCommand = (new[]
                {
                    mainModel.CountReplaced.Select(x => x > 0),
                    mainModel.CountConflicted.Select(x => x <= 0),
                    IsIdle,
                })
                .CombineLatestValuesAreAllTrue()
                .ObserveOn(uiScheduler)
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => RenameExecute());

            this.ShowInformationPageCommand = IsIdle
                .ToReactiveCommand()
                .WithSubscribe(() => ShowDialog(new InformationPageViewModel()));

            this.ShowHelpPageCommand = IsIdle
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => mainModel.ShowHelpHtml());

            this.SettingVM = mainModel.ObserveProperty(x => x.Setting)
                .Select(_ => new SettingAppViewModel(mainModel))
                .ObserveOn(uiScheduler)
                .ToReadOnlyReactivePropertySlim<SettingAppViewModel>();

            this.LoadFilesFromDialogCommand = IsIdle
                .ToAsyncReactiveCommand<FolderSelectionMessage>()
                .WithSubscribe(async x => await LoadFileFromDialog(x));

            this.LoadFilesFromNewPathCommand = IsIdle
                .ToAsyncReactiveCommand<IReadOnlyList<string>>()
                .WithSubscribe(async x => await LoadFileFromNewPath(x));

            this.LoadFilesFromCurrentPathCommand = IsIdle
                .ToAsyncReactiveCommand()
                .WithSubscribe(LoadFilesFromCurrentPath);

            this.AddFilesFromDialogCommand = IsIdle
                .ToReactiveCommand<OpeningFileSelectionMessage>()
                .WithSubscribe(x => mainModel.AddTargetFiles(x.Response ?? Array.Empty<string>()));

            //アプリケーション内メッセージをダイアログで表示する
            mainModel.MessageEventStream
                //同種類の警告をまとめるため、時間でバッファ
                .Buffer(TimeSpanMessageBuffer)
                .Where(ms => ms.Any())
                //同じヘッダのメッセージをまとめる
                .Select(ms => ms
                    .SumSameHead()
                    .Select(m => new MessageDialogViewModel(m)))
                .ObserveOn(uiScheduler)
                .Subscribe(async ms =>
                {
                    foreach (var m in ms)
                        await ShowDialogAsync(m);
                });

            //ユーザーへの確認はダイアログで行う
            this.mainModel.ConfirmUser += async () =>
             {
                 var confirmDialogViewModel = new ConfirmDialogViewModel();
                 confirmDialogViewModel.IsOkResult.Skip(1)
                    .Subscribe(_ => IsDialogOpen.Value = false);

                 await ShowDialogAsync(confirmDialogViewModel, false);
                 return confirmDialogViewModel.IsOkResult.Value == true;
             };
        }

        private void ShowDialog(ViewModel innerVM, bool canCloseAwayDialog = true)
        {
            DialogContentVM.Value = innerVM;
            CloseOnClickAwayDialog.Value = canCloseAwayDialog;
            IsDialogOpen.Value = true;
        }
        private async Task ShowDialogAsync(ViewModel innerVM, bool canCloseAwayDialog = true)
        {
            ShowDialog(innerVM, canCloseAwayDialog);
            await IsDialogOpen.WaitUntilValueChangedAsync();
        }

        private Task LoadFileFromDialog(FolderSelectionMessage fsMessage) =>
           (fsMessage.Response?.Any(x => x.HasText()) == true)
                ? LoadFileFromNewPath(fsMessage.Response)
                : Task.CompletedTask;

        private Task LoadFileFromNewPath(IReadOnlyList<string> targetPaths)
        {
            mainModel.Setting.SearchFilePaths = targetPaths;
            return LoadFilesFromCurrentPath();
        }

        private async Task LoadFilesFromCurrentPath()
        {
            //ファイル読込を開始する
            Task taskLoad = mainModel.LoadFileElements();

            //一定時間経過しても処理が終了していなかったら、
            await Task.WhenAny(Task.Delay(500), taskLoad);
            if (taskLoad.IsCompleted)
                return;

            //進行ダイアログを表示
            var innerVM = new ProgressDialogViewModel(mainModel);
            ShowDialog(innerVM, false);
            await taskLoad;
            //読込が終わったらダイアログを閉じる
            IsDialogOpen.Value = false;
        }

        private async Task RenameExecute()
        {
            //リネーム実行を開始する
            Task taskRename = mainModel.RenameExecute();

            //一定時間経過しても処理が終了していなかったら、
            await Task.WhenAny(Task.Delay(500), taskRename);
            if (taskRename.IsCompleted)
                return;

            //進行ダイアログを表示
            var innerVM = new ProgressDialogViewModel(mainModel);
            ShowDialog(innerVM, false);
            await taskRename;
            //読込が終わったらダイアログを閉じる
            IsDialogOpen.Value = false;
        }

        /// <summary>
        /// アプリケーション起動時処理
        /// </summary>
        public void Initialize()
        {
            mainModel.Initialize();
            LogTo.Information("App Initialized");
        }

        /// <summary>
        /// アプリケーション終了時処理
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mainModel.SaveSettingFile(SettingAppModel.DefaultFilePath);
            }

            base.Dispose(disposing);

            LogTo.Information("App is Ended");
        }
    }
}
