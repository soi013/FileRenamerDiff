using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Collections;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Metadata;

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
using Serilog.Events;

using FileRenamerDiff.Models;
using DiffPlex;

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// 進行状態表示用VM
    /// </summary>
    public class ProgressDialogViewModel : DialogBaseViewModel
    {
        public IReadOnlyReactiveProperty<ProgressInfo> CurrentProgessInfo { get; }

        public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();

        public ProgressDialogViewModel()
        {
            this.CurrentProgessInfo = model.CurrentProgessInfo
                .Buffer(TimeSpan.FromMilliseconds(500))
                .Where(x => x.Any())
                .Select(x => x.Last())
                .ObserveOnUIDispatcher()
                .ToReadOnlyReactivePropertySlim();

            CancelCommand
                .Subscribe(() => model.CancelWork?.Cancel());
        }
    }
}
