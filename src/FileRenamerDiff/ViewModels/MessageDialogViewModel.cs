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
}
