using Anotar.Serilog;
using FileRenamerDiff.Models;
using Livet;
using Livet.Messaging.IO;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

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
        /// ファイルVMコレクションを含んだDataGrid用VM
        /// </summary>
        public FileElementsGridViewModel GridVM { get; } = new FileElementsGridViewModel();

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
        /// アプリケーション情報表示コマンド
        /// </summary>
        public ReactiveCommand ShowInformationPageCommand { get; }

        /// <summary>
        /// 設定情報ViewModel
        /// </summary>
        public ReadOnlyReactivePropertySlim<SettingAppViewModel> SettingVM { get; }


        public MainWindowViewModel()
        {
            this.ReplaceCommand = new[]
                {
                    model.ObserveProperty(x => x.FileElementModels).Select(x => x?.Count() >= 1),
                    IsIdle
                }
                .CombineLatestValuesAreAllTrue()
                .ObserveOnUIDispatcher()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.Replace());

            this.RenameExcuteCommand = (new[]
                {
                    model.CountReplaced.Select(x => x > 0),
                    model.CountConflicted.Select(x => x <= 0),
                    IsIdle,
                })
                .CombineLatestValuesAreAllTrue()
                .ObserveOnUIDispatcher()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => RenameExcute());

            this.ShowInformationPageCommand = IsIdle
                .ToReactiveCommand()
                .WithSubscribe(() => ShowDialog(new InformationPageViewModel()));

            this.SettingVM = model.ObserveProperty(x => x.Setting)
                .Select(x => new SettingAppViewModel(x))
                .ObserveOnUIDispatcher()
                .ToReadOnlyReactivePropertySlim();

            this.LoadFilesFromDialogCommand = IsIdle
                .ToAsyncReactiveCommand<FolderSelectionMessage>()
                .WithSubscribe(async x => await LoadFileFromDialog(x));

            this.LoadFilesFromNewPathCommand = IsIdle
                .ToAsyncReactiveCommand<string>()
                .WithSubscribe(async x => await LoadFileFromNewPath(x));

            this.LoadFilesFromCurrentPathCommand = IsIdle
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