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
    /// ダイアログ表示用VMの基底クラス
    /// </summary>
    public class DialogBaseViewModel : ViewModel
    {
        protected Model model = Model.Instance;

        /// <summary>
        /// ダイアログが表示されているか
        /// Bindされるのは使用側のDialogHost
        /// </summary>
        public ReactivePropertySlim<bool> IsDialogOpen { get; } = new(false);
    }
}
