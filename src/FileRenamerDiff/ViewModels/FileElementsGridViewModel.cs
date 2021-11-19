using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Data;

using Anotar.Serilog;

using FileRenamerDiff.Models;

using Livet;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace FileRenamerDiff.ViewModels;

/// <summary>
/// ファイル情報VMコレクションを含んだDataGrid用VM
/// </summary>
public class FileElementsGridViewModel : ViewModel
{
    /// <summary>
    /// ファイル情報コレクションのDataGrid用のICollectionView
    /// </summary>
    public ICollectionView CViewFileElementVMs { get; }

    internal readonly ReadOnlyReactiveCollection<FileElementViewModel> fileElementVMs;

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
    public ReactivePropertySlim<bool> IsVisibleReplacedOnly { get; } = new(false);

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
    public ReactivePropertySlim<bool> IsVisibleConflictedOnly { get; } = new(false);

    /// <summary>
    /// ファイルが1つでもあるか
    /// </summary>
    public ReadOnlyReactivePropertySlim<bool> IsAnyFiles { get; }
    /// <summary>
    /// ファイルの数
    /// </summary>
    public ReadOnlyReactivePropertySlim<int> CountFiles { get; }

    /// <summary>
    /// 直接ファイル追加
    /// </summary>
    public ReactiveCommand<IReadOnlyList<string>> AddTargetFilesCommand { get; }
    /// <summary>
    /// ファイルリストの全消去
    /// </summary>
    public ReactiveCommand ClearFileElementsCommand { get; }
    /// <summary>
    /// ファイルからの削除
    /// </summary>
    public ReactiveCommand<FileElementViewModel> RemoveItemCommand { get; } = new();

    /// <summary>
    /// デザイナー用です　コードからは呼べません
    /// </summary>
    [Obsolete("Designer only", true)]
    public FileElementsGridViewModel() : this(DesignerModel.MainModelForDesigner) { }

    public FileElementsGridViewModel(MainModel mainModel)
    {
        IScheduler uiScheduler = mainModel.UIScheduler;
        this.CountReplaced = mainModel.CountReplaced.ObserveOn(uiScheduler).ToReadOnlyReactivePropertySlim();
        this.IsReplacedAny = CountReplaced.Select(x => x > 0).ToReadOnlyReactivePropertySlim();
        this.CountConflicted = mainModel.CountConflicted.ObserveOn(uiScheduler).ToReadOnlyReactivePropertySlim();
        this.IsNotConflictedAny = CountConflicted.Select(x => x <= 0).ToReadOnlyReactivePropertySlim();

        this.fileElementVMs = mainModel.FileElementModels
            .ToReadOnlyReactiveCollection(x => new FileElementViewModel(x), uiScheduler);

        this.CViewFileElementVMs = CreateCollectionViewFilePathVMs(fileElementVMs);

        //表示基準に変更があったら、表示判定対象に変更があったら、CollectionViewの表示を更新する
        new[]
        {
                this.IsVisibleReplacedOnly,
                this.IsVisibleConflictedOnly,
                this.CountConflicted.Select(_=>true),
                this.CountReplaced.Select(_=>true),
            }
        .Merge()
        .Throttle(TimeSpan.FromMilliseconds(100))
        .ObserveOn(uiScheduler)
        .Subscribe(_ => RefleshCollectionViewSafe());

        this.IsReplacedAny
            .Where(x => x == false)
            .Subscribe(_ =>
                this.IsVisibleReplacedOnly.Value = false);

        AddTargetFilesCommand = mainModel.IsIdleUI
            .ToReactiveCommand<IReadOnlyList<string>>()
            .WithSubscribe(x => mainModel.AddTargetFiles(x));

        this.IsAnyFiles = mainModel.FileElementModels.ObserveIsAny().ToReadOnlyReactivePropertySlim();
        this.CountFiles = mainModel.FileElementModels.ObserveCount().ToReadOnlyReactivePropertySlim();

        this.ClearFileElementsCommand =
            (new[]
            {
                    mainModel.IsIdle,
                    IsAnyFiles,
            })
            .CombineLatestValuesAreAllTrue()
            .ObserveOn(uiScheduler)
            .ToReactiveCommand()
            .WithSubscribe(() => mainModel.FileElementModels.Clear());

        RemoveItemCommand = mainModel.IsIdleUI
            .ToReactiveCommand<FileElementViewModel>()
            .WithSubscribe(x =>
                mainModel.FileElementModels.Remove(x.PathModel));
    }

    private ICollectionView CreateCollectionViewFilePathVMs(object fVMs)
    {
        ICollectionView cView = CollectionViewSource.GetDefaultView(fVMs);
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
        if (row is not FileElementViewModel pathVM)
            return true;

        bool replacedVisible = !IsVisibleReplacedOnly.Value || pathVM.IsReplaced.Value;
        bool conflictedVisible = !IsVisibleConflictedOnly.Value || pathVM.IsConflicted.Value;

        return replacedVisible && conflictedVisible;
    }

    private void RefleshCollectionViewSafe()
    {
        if (CViewFileElementVMs is not ListCollectionView currentView)
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
