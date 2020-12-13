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
using System.Threading.Tasks;

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
using Anotar.Serilog;

using FileRenamerDiff.Models;
using FileRenamerDiff.Properties;

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// ファイル情報VMコレクションを含んだDataGrid用VM
    /// </summary>
    public class FileElementsGridViewModel : ViewModel
    {
        readonly Model model = Model.Instance;

        /// <summary>
        /// ファイル情報コレクションのDataGrid用のICollectionView
        /// </summary>
        public ReadOnlyReactivePropertySlim<ICollectionView?> CViewFileElementVMs { get; }

        /// <summary>
        /// リネーム前後での変更があったファイル数
        /// </summary>
        public IReadOnlyReactiveProperty<int> CountReplaced { get; }
        /// <summary>
        /// リネーム前後で変更が１つでのあったか
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsReplacedAny { get; }

        /// <summary>
        /// 置換前後で差があったファイルのみ表示するか
        /// </summary>
        public ReactivePropertySlim<bool> IsVisibleReplacedOnly { get; } = new ReactivePropertySlim<bool>(false);

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

        public FileElementsGridViewModel()
        {
            this.CountReplaced = model.CountReplaced.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();
            this.IsReplacedAny = CountReplaced.Select(x => x > 0).ToReadOnlyReactivePropertySlim();
            this.CountConflicted = model.CountConflicted.ObserveOnUIDispatcher().ToReadOnlyReactivePropertySlim();
            this.IsNotConflictedAny = CountConflicted.Select(x => x <= 0).ToReadOnlyReactivePropertySlim();

            this.CViewFileElementVMs = model
                .ObserveProperty(x => x.FileElementModels)
                .Select(x => CreateFilePathVMs(x))
                .ObserveOnUIDispatcher()
                .Select(x => CreateCollectionViewFilePathVMs(x))
                .ToReadOnlyReactivePropertySlim();

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
        }

        private static ObservableCollection<FileElementViewModel> CreateFilePathVMs(IEnumerable<FileElementModel> fModels) =>
             new ObservableCollection<FileElementViewModel>(fModels.Select(fModel => new FileElementViewModel(fModel)));

        private ICollectionView CreateCollectionViewFilePathVMs(ObservableCollection<FileElementViewModel> fVMs)
        {
            var cView = CollectionViewSource.GetDefaultView(fVMs);
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

            var replacedVisible = !IsVisibleReplacedOnly.Value || pathVM.IsReplaced.Value;
            var conflictedVisible = !IsVisibleConflictedOnly.Value || pathVM.IsConflicted.Value;

            return replacedVisible && conflictedVisible;
        }

        private void RefleshCollectionViewSafe()
        {
            if (!(CViewFileElementVMs?.Value is ListCollectionView currentView))
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
    }
}