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
    public class DialogBaseViewModel : ViewModel
    {
        protected Model model = Model.Instance;


        /// <summary>
        /// ダイアログが表示されているか
        /// Bindされるのは使用側のDialogHost
        /// </summary>
        public ReactivePropertySlim<bool> IsDialogOpen { get; } = new ReactivePropertySlim<bool>(false);
    }
}
