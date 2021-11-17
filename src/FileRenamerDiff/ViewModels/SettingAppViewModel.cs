using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

using Microsoft.Extensions.DependencyInjection;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace FileRenamerDiff.ViewModels;

/// <summary>
/// 設定情報を編集するViewModel
/// </summary>
public class SettingAppViewModel : ViewModel
{
    private readonly SettingAppModel setting;

    /// <summary>
    /// リネームファイルを検索するターゲットパスリスト
    /// </summary>
    public ReactiveProperty<IReadOnlyList<string>> SearchFilePaths { get; }
    /// <summary>
    /// 連結されたリネームファイルを検索するターゲットパスリスト
    /// </summary>
    public ReactiveProperty<string> ConcatedSearchFilePaths { get; }

    /// <summary>
    /// 検索時に無視される拡張子コレクション
    /// </summary>
    public ObservableCollection<ValueHolder<string>> IgnoreExtensions => setting.IgnoreExtensions;

    /// <summary>
    /// 削除文字列パターン
    /// </summary>
    public ObservableCollection<ReplacePatternViewModel> DeleteTexts { get; }

    /// <summary>
    /// よく使う削除パターン集
    /// </summary>
    public IReadOnlyList<CommonPatternViewModel> CommonDeletePatternVMs { get; }

    /// <summary>
    /// 置換文字列パターン
    /// </summary>
    public ObservableCollection<ReplacePatternViewModel> ReplaceTexts { get; }

    /// <summary>
    /// よく使う置換パターン集
    /// </summary>
    public IReadOnlyList<CommonPatternViewModel> CommonReplacePatternVMs { get; }

    /// <summary>
    /// ファイル探索時にサブディレクトリを探索するか
    /// </summary>
    public ReactiveProperty<bool> IsSearchSubDirectories { get; }

    /// <summary>
    /// ディレクトリをリネーム対象にするか
    /// </summary>
    public ReactiveProperty<bool> IsDirectoryRenameTarget { get; }

    /// <summary>
    /// ディレクトリでないファイルをリネーム対象にするか
    /// </summary>
    public ReactiveProperty<bool> IsFileRenameTarget { get; }

    /// <summary>
    /// 隠しファイルをリネーム対象にするか
    /// </summary>
    public ReactiveProperty<bool> IsHiddenRenameTarget { get; }

    /// <summary>
    /// 拡張子をリネームするか
    /// </summary>
    public ReactiveProperty<bool> IsRenameExt { get; }

    /// <summary>
    /// 選択可能な言語一覧
    /// </summary>
    public IReadOnlyList<CultureInfo> AvailableLanguages { get; }

    /// <summary>
    /// アプリケーションの表示言語
    /// </summary>
    public ReactivePropertySlim<CultureInfo> SelectedLanguage { get; } = new();

    /// <summary>
    /// アプリケーションの色テーマ
    /// </summary>
    public ReactiveProperty<bool> IsAppDarkTheme { get; }

    /// <summary>
    /// 変更時に変更前後の履歴を保存するか
    /// </summary>
    public ReactiveProperty<bool> IsCreateRenameLog { get; }
    public ReactiveCommand AddIgnoreExtensionsCommand { get; }

    public AsyncReactiveCommand ClearIgnoreExtensionsCommand { get; }

    public ReactiveCommand AddDeleteTextsCommand { get; }
    public AsyncReactiveCommand ClearDeleteTextsCommand { get; }
    public ReactiveCommand AddReplaceTextsCommand { get; }
    public AsyncReactiveCommand ClearReplaceTextsCommand { get; }

    public AsyncReactiveCommand ShowExpressionReferenceCommand { get; }

    /// <summary>
    /// 設定初期化コマンド
    /// </summary>
    public ReactiveCommand ResetSettingCommand { get; }

    /// <summary>
    /// ファイル選択メッセージでの設定ファイル読込コマンド
    /// </summary>
    public ReactiveCommand<FileSelectionMessage> LoadSettingFileDialogCommand { get; }

    /// <summary>
    /// ファイル選択メッセージでの設定ファイル保存コマンド
    /// </summary>
    public ReactiveCommand<FileSelectionMessage> SaveSettingFileDialogCommand { get; }

    /// <summary>
    /// 前回保存設定ファイルディレクトリパス
    /// </summary>
    public IReadOnlyReactiveProperty PreviousSettingFileDirectory { get; }

    /// <summary>
    /// 前回保存設定ファイル名
    /// </summary>
    public IReadOnlyReactiveProperty PreviousSettingFileName { get; }

    /// <summary>
    /// デザイナー用です　コードからは呼べません
    /// </summary>
    [Obsolete("Designer only", true)]
    public SettingAppViewModel() : this(DesignerModel.MainModelForDesigner) { }

    public SettingAppViewModel(MainModel mainModel)
    {
        this.setting = mainModel.Setting;

        this.DeleteTexts = setting.DeleteTexts
                .ToObservableCollctionSynced(
                    x => new ReplacePatternViewModel(x),
                    x => x.ToReplacePattern());

        this.CommonDeletePatternVMs = CommonPattern.DeletePatterns
                .Select(x => new CommonPatternViewModel(mainModel, x, true))
                .ToArray();

        this.ReplaceTexts = setting.ReplaceTexts
            .ToObservableCollctionSynced(
                x => new ReplacePatternViewModel(x),
                x => x.ToReplacePattern());

        this.CommonReplacePatternVMs = CommonPattern.ReplacePatterns
            .Select(x => new CommonPatternViewModel(mainModel, x, false))
            .ToArray();

        this.SearchFilePaths = setting.ToReactivePropertyAsSynchronized(x => x.SearchFilePaths);
        this.ConcatedSearchFilePaths = setting.ToReactivePropertyAsSynchronized(x => x.ConcatedSearchFilePaths);
        this.IsSearchSubDirectories = setting.ToReactivePropertyAsSynchronized(x => x.IsSearchSubDirectories);
        this.IsDirectoryRenameTarget = setting.ToReactivePropertyAsSynchronized(x => x.IsDirectoryRenameTarget);
        this.IsFileRenameTarget = setting.ToReactivePropertyAsSynchronized(x => x.IsFileRenameTarget);
        this.IsHiddenRenameTarget = setting.ToReactivePropertyAsSynchronized(x => x.IsHiddenRenameTarget);
        this.IsRenameExt = setting.ToReactivePropertyAsSynchronized(x => x.IsRenameExt);

        this.IsAppDarkTheme = setting.ToReactivePropertyAsSynchronized(x => x.IsAppDarkTheme);
        this.IsCreateRenameLog = setting.ToReactivePropertyAsSynchronized(x => x.IsCreateRenameLog);

        this.AvailableLanguages = SettingAppModel.AvailableLanguages;
        this.SelectedLanguage = CreateAppLanguageRp();

        this.AddIgnoreExtensionsCommand = mainModel.IsIdleUI
             .ToReactiveCommand()
             .WithSubscribe(() => setting.AddIgnoreExtensions())
             .AddTo(this.CompositeDisposable);

        this.ClearIgnoreExtensionsCommand =
            new[]
            {
                    mainModel.IsIdleUI,
                    setting.IgnoreExtensions.ObserveIsAny(),
            }
            .CombineLatestValuesAreAllTrue()
            .ToAsyncReactiveCommand()
            .WithSubscribe(() =>
                mainModel.ExecuteAfterConfirm(() =>
                    setting.IgnoreExtensions.Clear()))
             .AddTo(this.CompositeDisposable);

        this.AddDeleteTextsCommand = mainModel.IsIdleUI
             .ToReactiveCommand()
             .WithSubscribe(() => setting.AddDeleteTexts())
             .AddTo(this.CompositeDisposable);
        this.ClearDeleteTextsCommand =
            new[]
            {
                    mainModel.IsIdleUI,
                    setting.DeleteTexts.ObserveIsAny(),
            }
            .CombineLatestValuesAreAllTrue()
             .ToAsyncReactiveCommand()
             .WithSubscribe(() =>
                mainModel.ExecuteAfterConfirm(() =>
                    setting.DeleteTexts.Clear()))
             .AddTo(this.CompositeDisposable);

        this.AddReplaceTextsCommand = mainModel.IsIdleUI
            .ToReactiveCommand()
            .WithSubscribe(() => setting.AddReplaceTexts())
            .AddTo(this.CompositeDisposable);
        this.ClearReplaceTextsCommand =
            new[]
            {
                    mainModel.IsIdleUI,
                    setting.ReplaceTexts.ObserveIsAny(),
            }
            .CombineLatestValuesAreAllTrue()
             .ToAsyncReactiveCommand()
             .WithSubscribe(() =>
                mainModel.ExecuteAfterConfirm(() =>
                    setting.ReplaceTexts.Clear()))
             .AddTo(this.CompositeDisposable);

        this.ShowExpressionReferenceCommand = mainModel.IsIdleUI
            .ToAsyncReactiveCommand()
            .WithSubscribe(() => mainModel.ShowHelpHtml())
            .AddTo(this.CompositeDisposable);

        this.ResetSettingCommand = mainModel.IsIdleUI
            .ToReactiveCommand()
            .WithSubscribe(() => mainModel.ResetSetting())
            .AddTo(this.CompositeDisposable);

        this.LoadSettingFileDialogCommand = mainModel.IsIdleUI
            .ToReactiveCommand<FileSelectionMessage>()
            .WithSubscribe(x => mainModel.LoadSettingFile(x.Response?.FirstOrDefault() ?? String.Empty));

        this.SaveSettingFileDialogCommand = mainModel.IsIdleUI
            .ToReactiveCommand<FileSelectionMessage>()
            .WithSubscribe(x => mainModel.SaveSettingFile(x.Response?.FirstOrDefault() ?? String.Empty));

        this.PreviousSettingFileDirectory = mainModel.PreviousSettingFilePath
            .Select(x => Path.GetDirectoryName(x))
            .ToReadOnlyReactivePropertySlim();
        this.PreviousSettingFileName = mainModel.PreviousSettingFilePath
            .Select(x => Path.GetFileName(x))
            .ToReadOnlyReactivePropertySlim();
    }

    private ReactivePropertySlim<CultureInfo> CreateAppLanguageRp()
    {
        //Model側から変更されることは無いはずなので、初期値のみ読込
        //リストにない言語の場合は、Autoに設定する
        var modelCultureInfo = CultureInfo.GetCultureInfo(setting.AppLanguageCode ?? "");
        if (!AvailableLanguages.Contains(modelCultureInfo))
            modelCultureInfo = CultureInfo.InvariantCulture;

        var rp = new ReactivePropertySlim<CultureInfo>(modelCultureInfo);

        rp.Select(c =>
                (c == CultureInfo.InvariantCulture)
                    ? String.Empty
                    : c.Name)
            .Subscribe(c => setting.AppLanguageCode = c);

        return rp;
    }
}
