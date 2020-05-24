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

using FileRenamerDiff.Models;

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// アプリケーション情報ダイアログVM
    /// </summary>
    public class InformationPageViewModel : DialogBaseViewModel
    {
        /// <summary>
        /// アプリケーション情報Markdown
        /// </summary>
        public static string AppInfoText { get; } = CreateAppInfoText();

        private static string CreateAppInfoText()
        {
            string author = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string url = @"https://github.com/soi013/FileRenamerDiff";

            var stb = new StringBuilder()
            .AppendLine("# File Renamer Diff")
            .AppendLine($"Made by *{author}*").AppendLine()
            .AppendLine($"Version *{version}*").AppendLine()
            .Append("Repository ").AppendLine(url);


            return stb.ToString();
        }
    }
}
