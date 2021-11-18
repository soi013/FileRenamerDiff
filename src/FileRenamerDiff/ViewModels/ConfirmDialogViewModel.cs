using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
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

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

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
