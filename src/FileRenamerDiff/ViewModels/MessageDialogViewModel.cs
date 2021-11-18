using FileRenamerDiff.Models;

using Livet;

namespace FileRenamerDiff.ViewModels;

/// <summary>
/// アプリケーション内メッセージ表示用VM
/// </summary>
public class MessageDialogViewModel : ViewModel
{
    public AppMessage AppMessage { get; }

    /// <summary>
    /// デザイナー用です　コードからは呼べません
    /// </summary>
    [Obsolete("Designer only", true)]
    public MessageDialogViewModel()
        : this(new(AppMessageLevel.Alert, head: "DUMMY HEAD", body: "DUMMY BODY")) { }

    public MessageDialogViewModel(AppMessage aMessage)
    {
        this.AppMessage = aMessage;
    }
}
