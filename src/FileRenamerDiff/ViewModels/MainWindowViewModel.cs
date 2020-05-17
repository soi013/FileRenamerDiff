using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Collections;
using System.Threading.Tasks;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Concurrency;
using ps = System.Reactive.PlatformServices;
using Anotar.Serilog;
using Serilog.Events;
using Microsoft.WindowsAPICodePack.Shell.Interop;

using FileRenamerDiff.Models;

namespace FileRenamerDiff.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        Model model = Model.Instance;

        /// <summary>
        /// アプリケーションが待機状態か
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsIdle => model.IsIdleUI;

        /// <summary>
        /// ダイアログ表示VM
        /// </summary>
        public ReactivePropertySlim<DialogBaseViewModel> DialogContentVM { get; set; } = new ReactivePropertySlim<DialogBaseViewModel>();
        /// <summary>
        /// ダイアログの外側をクリックした際に閉じられるか
        /// </summary>
        public ReactivePropertySlim<bool> CloseOnClickAwayDialog { get; } = new ReactivePropertySlim<bool>(true);

        /// <summary>
        /// ファイル情報コレクションのDataGrid用のICollectionView
        /// </summary>
        public ReadOnlyReactivePropertySlim<ICollectionView> CViewFileElementVMs { get; }
        private ReadOnlyReactivePropertySlim<ObservableCollection<FileElementViewModel>> fileElementVMs;

        /// <summary>
        /// フォルダ選択メッセージでのフォルダ読込コマンド
        /// </summary>
        public AsyncReactiveCommand<FolderSelectionMessage> LoadFilesFromDialogCommand { get; }
        /// <summary>
        /// パス指定でのフォルダ読込コマンド
        /// </summary>
        public AsyncReactiveCommand<string> LoadFilesFromNewPathCommand { get; }
        /// <summary>
        /// フォルダ読み込みコマンド
        /// </summary>
        public AsyncReactiveCommand LoadFilesFromCurrentPathCommand { get; }

        /// <summary>
        /// 置換実行コマンド
        /// </summary>
        public AsyncReactiveCommand ReplaceCommand { get; }

        /// <summary>
        /// 置換後ファイル名保存コマンド
        /// </summary>
        public AsyncReactiveCommand RenameExcuteCommand { get; }

        /// <summary>
        /// 置換前後で差があったファイルのみ表示するか
        /// </summary>
        public ReactivePropertySlim<bool> IsVisibleReplacedOnly { get; } = new ReactivePropertySlim<bool>(false);

        /// <summary>
        /// アプリケーション情報表示コマンド
        /// </summary>
        public ReactiveCommand ShowInformationPageCommand { get; }

        /// <summary>
        /// 設定情報ViewModel
        /// </summary>
        public ReadOnlyReactivePropertySlim<SettingAppViewModel> SettingVM { get; }

        /// <summary>
        /// リネーム前後での変更があったファイル数
        /// </summary>
        public IReadOnlyReactiveProperty<int> CountReplaced { get; }
        /// <summary>
        /// リネーム前後で変更が１つでのあったか
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsReplacedAny { get; }

        /// <summary>
        /// ファイルパスの衝突しているファイル数
        /// </summary>
        public IReadOnlyReactiveProperty<int> CountConflicted { get; }

        /// <summary>
        /// ファイルパスの衝突がないか
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsNotConflictedAny { get; }

        /// <summary>
        /// ファイルパスが衝突しているファイルのみ表示するか
        /// </summary>
        public ReactivePropertySlim<bool> IsVisibleConflictedOnly { get; } = new ReactivePropertySlim<bool>(false);

        public MainWindowViewModel()
        {
            this.CountReplaced = model.CountReplaced.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();
            this.IsReplacedAny = CountReplaced.Select(x => x > 0).ToReadOnlyReactivePropertySlim();
            this.CountConflicted = model.CountConflicted.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();
            this.IsNotConflictedAny = CountConflicted.Select(x => x <= 0).ToReadOnlyReactivePropertySlim();

            this.fileElementVMs = model.ObserveProperty(x => x.FileElementModels)
                .Select(x => CreateFilePathVMs(x))
                .ToReadOnlyReactivePropertySlim();

            this.CViewFileElementVMs = fileElementVMs
                .Select(x => CreateCollectionViewFilePathVMs(x))
                .ToReadOnlyReactivePropertySlim();

            this.ReplaceCommand = new[]
                {
                    fileElementVMs.Select(x => x?.Count()>=1),
                    IsIdle
                }
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.Replace());

            this.RenameExcuteCommand = (new[]
                {
                    IsReplacedAny,
                    IsNotConflictedAny,
                    IsIdle,
                })
                .CombineLatestValuesAreAllTrue()
                .ObserveOnUIDispatcher()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => RenameExcute());

            this.ShowInformationPageCommand = IsIdle
                .ToReactiveCommand()
                .WithSubscribe(() => ShowDialog(new InformationPageViewModel()));

            //表示基準に変更があったら、表示判定対象に変更があったら、CollectionViewの表示を更新する
            new[]
            {
                this.IsVisibleReplacedOnly,
                this.IsVisibleConflictedOnly,
                this.CountConflicted.Select(_=>true),
                this.CountReplaced.Select(_=>true),
            }
            .CombineLatest()
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOnUIDispatcher()
            .Subscribe(_ => RefleshCollectionViewSafe());

            this.SettingVM = model.ObserveProperty(x => x.Setting)
                .Select(x => new SettingAppViewModel(x))
                .ToReadOnlyReactivePropertySlim();

            this.LoadFilesFromDialogCommand = IsIdle
                .ToAsyncReactiveCommand<FolderSelectionMessage>()
                .WithSubscribe(async x => await LoadFileFromDialog(x));

            this.LoadFilesFromNewPathCommand = IsIdle
                .ToAsyncReactiveCommand<string>()
                .WithSubscribe(async x => await LoadFileFromNewPath(x));

            this.LoadFilesFromCurrentPathCommand = (new[]
                {
                    SettingVM.Value.SearchFilePath.Select(x => !string.IsNullOrWhiteSpace(x)),
                    IsIdle
                })
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .WithSubscribe(LoadFilesFromCurrentPath);

            //アプリケーション内メッセージをダイアログで表示する
            model.MessageEventStream
                .Where(m => m != null)
                //同種類の警告をまとめるため、時間でバッファ
                .Buffer(TimeSpan.FromMilliseconds(100))
                .Where(ms => ms.Any())
                //同じヘッダのメッセージをまとめる
                .Select(ms => ms
                    .SumSameHead()
                    .Select(m => new MessageDialogViewModel(m)))
                .ObserveOnUIDispatcher()
                .Subscribe(async ms =>
                {
                    foreach (var m in ms)
                        await ShowDialogAsync(m);
                });

            //ユーザーへの確認はダイアログで行う
            this.model.ConfirmUser += async () =>
             {
                 var confirmDialogViewModel = new ConfirmDialogViewModel();
                 await ShowDialogAsync(confirmDialogViewModel, false);
                 return confirmDialogViewModel.IsOK == true;
             };
        }

        private void ShowDialog(DialogBaseViewModel innerVM, bool canCloseAwayDialog = true)
        {
            this.DialogContentVM.Value = innerVM;
            this.CloseOnClickAwayDialog.Value = canCloseAwayDialog;
            innerVM.IsDialogOpen.Value = true;
        }
        private async Task ShowDialogAsync(DialogBaseViewModel innerVM, bool canCloseAwayDialog = true)
        {
            ShowDialog(innerVM, canCloseAwayDialog);
            await innerVM.IsDialogOpen;
        }

        private async Task LoadFileFromDialog(FolderSelectionMessage fsMessage)
        {
            if (fsMessage.Response == null)
                return;
            await LoadFileFromNewPath(fsMessage.Response);
        }

        private async Task LoadFileFromNewPath(string targetPath)
        {
            SettingVM.Value.SearchFilePath.Value = targetPath;
            await LoadFilesFromCurrentPath();
        }

        private async Task LoadFilesFromCurrentPath()
        {
            //ファイル読込を開始する
            var taskLoad = model.LoadFileElements();

            //一定時間経過しても処理が終了していなかったら、
            await Task.WhenAny(Task.Delay(500), taskLoad);
            if (taskLoad.IsCompleted)
                return;

            //進行ダイアログを表示
            var innerVM = new ProgressDialogViewModel();
            ShowDialog(innerVM, false);
            await taskLoad;
            //読込が終わったらダイアログを閉じる
            innerVM.IsDialogOpen.Value = false;
        }

        private async Task RenameExcute()
        {
            //リネーム実行を開始する
            var taskRename = model.RenameExcute();

            //一定時間経過しても処理が終了していなかったら、
            await Task.WhenAny(Task.Delay(500), taskRename);
            if (taskRename.IsCompleted)
                return;

            //進行ダイアログを表示
            var innerVM = new ProgressDialogViewModel();
            ShowDialog(innerVM, false);
            await taskRename;
            //読込が終わったらダイアログを閉じる
            innerVM.IsDialogOpen.Value = false;
        }

        private ObservableCollection<FileElementViewModel> CreateFilePathVMs(IEnumerable<FileElementModel> paths)
        {
            if (paths == null)
                return null;
            return new ObservableCollection<FileElementViewModel>(paths.Select(path => new FileElementViewModel(path)));
        }

        private ICollectionView CreateCollectionViewFilePathVMs(ObservableCollection<FileElementViewModel> vms)
        {
            var cView = CollectionViewSource.GetDefaultView(vms);
            cView.Filter = (x => GetVisibleRow(x));
            return cView;
        }

        /// <summary>
        /// 2つの表示切り替えプロパティと、各行の値に応じて、その行の表示状態を決定する
        /// </summary>
        /// <param name="row">行VM</param>
        /// <returns>表示状態</returns>
        private bool GetVisibleRow(object row)
        {
            if (!(row is FileElementViewModel pathVM))
                return true;

            var replacedVisible = IsVisibleReplacedOnly.Value
                ? pathVM.IsReplaced.Value
                : true;


            var conflictedVisible = IsVisibleConflictedOnly.Value
                ? pathVM.IsConflicted.Value
                : true;

            return replacedVisible && conflictedVisible;
        }

        private void RefleshCollectionViewSafe()
        {
            if (!(CViewFileElementVMs.Value is ListCollectionView currentView))
                return;

            //なぜかCollectionViewが追加中・編集中のことがある。
            if (currentView.IsAddingNew)
            {
                LogTo.Warning("CollectionView is Adding");
                currentView.CancelNew();
            }
            if (currentView.IsEditingItem)
            {
                LogTo.Warning("CollectionView is Editing");
                currentView.CommitEdit();
            }

            currentView.Refresh();
        }

        /// <summary>
        /// アプリケーション起動時処理
        /// </summary>
        public void Initialize()
        {
            model.Initialize();
            LogTo.Information("App Initialized");
        }

        /// <summary>
        /// アプリケーション終了時処理
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                model.SaveSetting();
            }

            base.Dispose(disposing);

            LogTo.Information("App is Ended");
        }
    }
}