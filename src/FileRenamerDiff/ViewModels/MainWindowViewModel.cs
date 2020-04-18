using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Collections;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;


using FileRenamerDiff.Models;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ps = System.Reactive.PlatformServices;

namespace FileRenamerDiff.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        Model model = Model.Instance;

        public ReadOnlyReactivePropertySlim<ICollectionView> FilePathVMs { get; }

        public ReactiveCommand<FolderSelectionMessage> FileLoadPathCommand { get; }
        public ReactiveCommand FileLoadCommand { get; }

        public AsyncReactiveCommand ReplaceCommand { get; }

        public AsyncReactiveCommand RenameExcuteCommand { get; }

        public ReactivePropertySlim<bool> IsVisibleReplacedOnly { get; } = new ReactivePropertySlim<bool>(false);
        public ReadOnlyReactivePropertySlim<SettingAppViewModel> SettingVM { get; }

        public MainWindowViewModel()
        {
            this.FilePathVMs = model.ObserveProperty(x => x.SourceFilePathVMs)
                .Select(x => CreateFilePathVMs(x))
                .ToReadOnlyReactivePropertySlim();

            this.ReplaceCommand = FilePathVMs
                .Select(x => x?.IsEmpty == false)
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.Replace());

            this.RenameExcuteCommand = model.IsReplacedAny
                .ObserveOnUIDispatcher()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => model.RenameExcute());

            this.IsVisibleReplacedOnly.Subscribe(
                _ => FilePathVMs.Value.Filter = (x => IsVisiblePath(x)));

            this.SettingVM = model.ObserveProperty(x => x.Setting)
                .Select(x => new SettingAppViewModel(x))
                .ToReadOnlyReactivePropertySlim();

            this.FileLoadPathCommand = new ReactiveCommand<FolderSelectionMessage>()
                .WithSubscribe(x =>
                {
                    SettingVM.Value.SourceFilePath.Value = x.Response;
                    model.LoadSourceFiles();
                });

            this.FileLoadCommand = this.SettingVM.Value.SourceFilePath
                .Select<string, bool>(x => !String.IsNullOrWhiteSpace(x))
                .ToReactiveCommand()
                .WithSubscribe(() => model.LoadSourceFiles());
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