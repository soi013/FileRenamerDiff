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
using System.IO;
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
    /// 進行状態表示用VM
    /// </summary>
    public class ProgressDialogViewModel : DialogBaseViewModel
    {
        public IReadOnlyReactiveProperty<ProgressInfo?> CurrentProgessInfo { get; }

        public AsyncReactiveCommand CancelCommand { get; }

        private readonly ReactivePropertySlim<bool> limitOneceCancel = new(true);

        /// <summary>
        /// デザイナー用です　コードからは呼べません
        /// </summary>
        [Obsolete("Designer only", true)]
        public ProgressDialogViewModel() : this(DesignerModel.MainModelForDesigner) { }

        public ProgressDialogViewModel(MainModel mainModel)
        {
            this.CurrentProgessInfo = mainModel.CurrentProgessInfo
                .Buffer(TimeSpan.FromMilliseconds(500))
                .Where(x => x.Any())
                .Select(x => x.Last())
                .ObserveOnUIDispatcher()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.CompositeDisposable);

            //ダブルクリックなどで2回以上キャンセルを押されるのを防ぐため、専用のプロパティを使用
            CancelCommand = limitOneceCancel
                .ToAsyncReactiveCommand()
                .WithSubscribe(() =>
                {
                    limitOneceCancel.Value = false;
                    mainModel.CancelWork?.Cancel();
                    return Task.CompletedTask;
                });
        }
    }
}
