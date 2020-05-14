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
namespace FileRenamerDiff.ViewModels
{
    public class ConfirmDialogViewModel : DialogBaseViewModel
    {
        /// <summary>
        /// ダイアログ結果（初期状態はNull）
        /// </summary>
        public bool? IsOK { get; private set; } = null;

        public ReactiveCommand OkCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();

        public ConfirmDialogViewModel()
        {
            OkCommand.Subscribe(() =>
            {
                IsOK = true;
                IsDialogOpen.Value = false;
            });

            CancelCommand.Subscribe(() =>
            {
                IsOK = false;
                IsDialogOpen.Value = false;
            });
        }
    }
}
