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

using FileRenamerDiff.Models;

namespace FileRenamerDiff.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        Model model = Model.Instance;
        public IReadOnlyReactiveProperty<bool> IsIdle { get; }

        public ReadOnlyReactivePropertySlim<ICollectionView> FilePathVMs { get; }

        public AsyncReactiveCommand<FolderSelectionMessage> FileLoadPathCommand { get; }
        public AsyncReactiveCommand FileLoadCommand { get; }

        public AsyncReactiveCommand ReplaceCommand { get; }

        public AsyncReactiveCommand RenameExcuteCommand { get; }

        public ReactivePropertySlim<bool> IsVisibleReplacedOnly { get; } = new ReactivePropertySlim<bool>(false);
        public ReadOnlyReactivePropertySlim<SettingAppViewModel> SettingVM { get; }

        public IReadOnlyReactiveProperty<int> CountReplaced { get; }
        public IReadOnlyReactiveProperty<bool> IsReplacedAny { get; }

        public MainWindowViewModel()
        {
            this.IsIdle = model.IsIdle.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();
            this.CountReplaced = model.CountReplaced.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();
            this.IsReplacedAny = CountReplaced.Select(x => x > 0).ToReadOnlyReactivePropertySlim();

            this.FilePathVMs = model.ObserveProperty(x => x.SourceFilePathVMs)
                .Select(x => CreateFilePathVMs(x))
                .ToReadOnlyReactivePropertySlim();

            this.ReplaceCommand = new[]
                {
                    FilePathVMs.Select(x => x?.IsEmpty == false),
                    IsIdle
                }
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.Replace());

            this.RenameExcuteCommand = new[]
                {
                    IsReplacedAny,
                    IsIdle
                }
                .CombineLatestValuesAreAllTrue()
                .ObserveOnUIDispatcher()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.RenameExcute());

            this.IsVisibleReplacedOnly.Subscribe(
                _ => FilePathVMs.Value.Filter = (x => IsVisiblePath(x)));

            this.SettingVM = model.ObserveProperty(x => x.Setting)
                .Select(x => new SettingAppViewModel(x))
                .ToReadOnlyReactivePropertySlim();

            this.FileLoadPathCommand = IsIdle
                .ToAsyncReactiveCommand<FolderSelectionMessage>()
                .WithSubscribe(async x => await FolderSelected(x));

            this.FileLoadCommand = new[]
                {
                    SettingVM.Value.SourceFilePath.Select<string, bool>(x => !String.IsNullOrWhiteSpace(x)),
                    IsIdle
                }
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.LoadSourceFiles());
        }

        private async Task FolderSelected(FolderSelectionMessage fsMessage)
        {
            if (fsMessage.Response == null)
                return;

            SettingVM.Value.SourceFilePath.Value = fsMessage.Response;
            await model.LoadSourceFiles();
        }

        private ICollectionView CreateFilePathVMs(IReadOnlyList<FilePathModel> paths)
        {
            if (paths == null)
                return null;

            var vms = new ObservableCollection<FilePathViewModel>(paths?.Select(path => new FilePathViewModel(path)));
            return CollectionViewSource.GetDefaultView(vms);
        }
        private bool IsVisiblePath(object row) =>
            !IsVisibleReplacedOnly.Value ? true
            : !(row is FilePathViewModel pathVM) ? true
            : pathVM.IsReplaced.Value;

        public void Initialize()
        {
            model.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                model.SaveSetting();
            }

            base.Dispose(disposing);
        }
    }
}