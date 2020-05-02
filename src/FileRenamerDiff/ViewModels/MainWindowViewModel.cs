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
using ps = System.Reactive.PlatformServices;
using Anotar.Serilog;

using FileRenamerDiff.Models;

namespace FileRenamerDiff.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        Model model = Model.Instance;

        /// <summary>
        /// アプリケーションが待機状態か
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsIdle { get; }

        /// <summary>
        /// ファイル情報コレクションのDataGrid用のICollectionView
        /// </summary>
        public ReadOnlyReactivePropertySlim<ICollectionView> FileElemenVMs { get; }

        /// <summary>
        /// フォルダ選択完了コマンド
        /// </summary>
        public AsyncReactiveCommand<FolderSelectionMessage> FileLoadPathCommand { get; }
        /// <summary>
        /// フォルダ読み込みコマンド
        /// </summary>
        public AsyncReactiveCommand FileLoadCommand { get; }

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
        /// ファイルパスの衝突がないか
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsNotConflictedAny { get; }

        /// <summary>
        /// ファイルパスが衝突しているファイルのみ表示するか
        /// </summary>
        //public ReactivePropertySlim<bool> IsVisibleConflictedOnly { get; } = new ReactivePropertySlim<bool>(false);

        public MainWindowViewModel()
        {
            this.IsIdle = model.IsIdle.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();
            this.CountReplaced = model.CountReplaced.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();
            this.IsReplacedAny = CountReplaced.Select(x => x > 0).ToReadOnlyReactivePropertySlim();
            this.IsNotConflictedAny = model.IsNotConflictedAny.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();

            this.FileElemenVMs = model.ObserveProperty(x => x.FileElementModels)
                .Select(x => CreateFilePathVMs(x))
                .ToReadOnlyReactivePropertySlim();

            this.ReplaceCommand = new[]
                {
                    FileElemenVMs.Select(x => x?.IsEmpty == false),
                    IsIdle
                }
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.Replace());

            this.RenameExcuteCommand = new[]
                {
                    IsReplacedAny,
                    IsNotConflictedAny,
                    IsIdle,
                }
                .CombineLatestValuesAreAllTrue()
                .ObserveOnUIDispatcher()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.RenameExcute());

            //表示基準に変更があったら、CollectionViewのフィルタを変更する
            this.IsVisibleReplacedOnly.Subscribe(
                _ => FileElemenVMs.Value.Filter = (x => IsVisiblePath(x)));

            this.SettingVM = model.ObserveProperty(x => x.Setting)
                .Select(x => new SettingAppViewModel(x))
                .ToReadOnlyReactivePropertySlim();

            this.FileLoadPathCommand = IsIdle
                .ToAsyncReactiveCommand<FolderSelectionMessage>()
                .WithSubscribe(async x => await FolderSelected(x));

            this.FileLoadCommand = new[]
                {
                    SettingVM.Value.SearchFilePath.Select<string, bool>(x => !String.IsNullOrWhiteSpace(x)),
                    IsIdle
                }
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.LoadFileElements());
        }

        private async Task FolderSelected(FolderSelectionMessage fsMessage)
        {
            if (fsMessage.Response == null)
                return;

            SettingVM.Value.SearchFilePath.Value = fsMessage.Response;
            await model.LoadFileElements();
        }

        private ICollectionView CreateFilePathVMs(IEnumerable<FileElementModel> paths)
        {
            if (paths == null)
                return null;

            var vms = new ObservableCollection<FileElementViewModel>(paths.Select(path => new FileElementViewModel(path)));
            return CollectionViewSource.GetDefaultView(vms);
        }

        private bool IsVisiblePath(object row) =>
            !IsVisibleReplacedOnly.Value ? true
            : !(row is FileElementViewModel pathVM) ? true
            : pathVM.IsReplaced.Value;

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