using Livet;

using Reactive.Bindings;

namespace FileRenamerDiff.ViewModels;

public class ConfirmDialogViewModel : ViewModel
{
    /// <summary>
    /// ダイアログ結果（初期状態はNull）
    /// </summary>
    public ReactivePropertySlim<bool?> IsOkResult { get; } = new ReactivePropertySlim<bool?>(null);

    public ReactiveCommand OkCommand { get; } = new();
    public ReactiveCommand CancelCommand { get; } = new();

    public ConfirmDialogViewModel()
    {
        OkCommand.Subscribe(() =>
            IsOkResult.Value = true);

        CancelCommand.Subscribe(() =>
            IsOkResult.Value = false);
    }
}
