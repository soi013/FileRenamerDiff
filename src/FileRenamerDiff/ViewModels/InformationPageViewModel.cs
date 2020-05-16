using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Collections;
using System.Threading.Tasks;

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

using FileRenamerDiff.Models;
using System.Reflection;
using System.Reflection.Metadata;

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// アプリケーション情報ダイアログVM
    /// </summary>
    public class InformationPageViewModel : DialogBaseViewModel
    {
        public static string Author { get; } = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
        public static string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// リソースファイルの文章を表示
        /// </summary>
        public static string LicenseText { get; } = Properties.Resources.License;
    }
}
