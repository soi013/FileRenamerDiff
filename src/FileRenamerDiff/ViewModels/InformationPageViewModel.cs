using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
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

namespace FileRenamerDiff.ViewModels;

/// <summary>
/// アプリケーション情報ダイアログVM
/// </summary>
public class InformationPageViewModel : ViewModel
{
    /// <summary>
    /// アプリケーション情報Markdown
    /// </summary>
    public static string AppInfoText { get; } = CreateAppInfoText();

    private static string CreateAppInfoText()
    {
        string? author = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
        string? version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        string url = @"https://github.com/soi013/FileRenamerDiff";
        string regexUrl = @"https://docs.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference#character-escapes";

        var stb = new StringBuilder()
        .AppendLine("# File Renamer Diff")
        .AppendLine($"Made by *{author}*  ")
        .AppendLine($"Version *{version}*  ")
        .AppendLine()
        .AppendLine($"Repository {url}  ")
        .AppendLine()
        .AppendLine($"Reference of Regex [Microsoft regular-expression-language-quick-reference]({regexUrl})  ");

        return stb.ToString();
    }
}
