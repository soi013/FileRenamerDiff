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
    /// アプリケーション内メッセージ表示用VM
    /// </summary>
    public class MessageDialogViewModel : ViewModel
    {
        Model model = Model.Instance;
        public AppMessage AppMessage { get; }

        /// <summary>
        /// デザイナー用です　コードからは呼べません
        /// </summary>
        [Obsolete("Designer only", true)]
        public MessageDialogViewModel()
            : this(new AppMessage(AppMessageLevel.Alert, head: "DUMMY HEAD", body: "DUMMY BODY")) { }

        public MessageDialogViewModel(AppMessage aMessage)
        {
            this.AppMessage = aMessage;
        }
    }
}
