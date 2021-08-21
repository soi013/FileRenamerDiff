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

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// 進行状態表示用VM
    /// </summary>
    public class ProgressDialogViewModel : ViewModel
    {
        public IReadOnlyReactiveProperty<ProgressInfo?> CurrentProgressInfo { get; }

        public AsyncReactiveCommand CancelCommand { get; }

        private readonly ReactivePropertySlim<bool> limitOneceCancel = new(true);

        /// <summary>
        /// デザイナー用です　コードからは呼べません
        /// </summary>
        [Obsolete("Designer only", true)]
        public ProgressDialogViewModel() : this(DesignerModel.MainModelForDesigner) { }

        public ProgressDialogViewModel(IMainModel mainModel)
        {
            this.CurrentProgressInfo = mainModel.CurrentProgressInfo
                .Buffer(TimeSpan.FromMilliseconds(500))
                .Where(x => x.Any())
                .Select(x => x.Last())
                .ObserveOn(mainModel.UIScheduler)
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
