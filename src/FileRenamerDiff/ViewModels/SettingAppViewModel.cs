using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive.Linq;

using FileRenamerDiff.Models;

using Livet;
using Livet.Messaging.IO;

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

    public AddSerialNumberViewModel AddSerialNumberVM { get; }

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
    /// <summary>
    /// 無視される拡張子コレクションに空白の要素を追加するコマンド
    /// </summary>
    public ReactiveCommand AddIgnoreExtensionsCommand { get; }
    /// <summary>
    /// 無視される拡張子コレクションの要素を消去するコマンド
    /// </summary>
    public AsyncReactiveCommand ClearIgnoreExtensionsCommand { get; }
    /// <summary>
    /// 削除文字列コレクションに空白の要素を追加するコマンド
    /// </summary>
    public ReactiveCommand AddDeleteTextsCommand { get; }
    /// <summary>
    /// 削除文字列コレクションの要素を消去するコマンド
    /// </summary>
    public AsyncReactiveCommand ClearDeleteTextsCommand { get; }
    /// <summary>
    /// 置換文字列コレクションに空白の要素を追加するコマンド
    /// </summary>
    public ReactiveCommand AddReplaceTextsCommand { get; }
    /// <summary>
    /// 置換文字列コレクションの要素を消去するコマンド
    /// </summary>
    public AsyncReactiveCommand ClearReplaceTextsCommand { get; }

    /// <summary>
    /// 正規表現リファレンスの表示コマンド
    /// </summary>
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

        this.AddSerialNumberVM = new AddSerialNumberViewModel(mainModel);

        this.SearchFilePaths = setting.ToReactivePropertyAsSynchronized(x => x.SearchFilePaths).AddTo(this.CompositeDisposable);
        this.ConcatedSearchFilePaths = setting.ToReactivePropertyAsSynchronized(x => x.ConcatedSearchFilePaths).AddTo(this.CompositeDisposable);
        this.IsSearchSubDirectories = setting.ToReactivePropertyAsSynchronized(x => x.IsSearchSubDirectories).AddTo(this.CompositeDisposable);
        this.IsDirectoryRenameTarget = setting.ToReactivePropertyAsSynchronized(x => x.IsDirectoryRenameTarget).AddTo(this.CompositeDisposable);
        this.IsFileRenameTarget = setting.ToReactivePropertyAsSynchronized(x => x.IsFileRenameTarget).AddTo(this.CompositeDisposable);
        this.IsHiddenRenameTarget = setting.ToReactivePropertyAsSynchronized(x => x.IsHiddenRenameTarget).AddTo(this.CompositeDisposable);
        this.IsRenameExt = setting.ToReactivePropertyAsSynchronized(x => x.IsRenameExt).AddTo(this.CompositeDisposable);

        this.IsAppDarkTheme = setting.ToReactivePropertyAsSynchronized(x => x.IsAppDarkTheme).AddTo(this.CompositeDisposable);
        this.IsCreateRenameLog = setting.ToReactivePropertyAsSynchronized(x => x.IsCreateRenameLog).AddTo(this.CompositeDisposable);

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
            .WithSubscribe(x => mainModel.LoadSettingFile(x.Response?.FirstOrDefault() ?? String.Empty))
            .AddTo(this.CompositeDisposable);

        this.SaveSettingFileDialogCommand = mainModel.IsIdleUI
            .ToReactiveCommand<FileSelectionMessage>()
            .WithSubscribe(x => mainModel.SaveSettingFile(x.Response?.FirstOrDefault() ?? String.Empty))
            .AddTo(this.CompositeDisposable);

        this.PreviousSettingFileDirectory = mainModel.PreviousSettingFilePath
            .Select(x => Path.GetDirectoryName(x))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(this.CompositeDisposable);

        this.PreviousSettingFileName = mainModel.PreviousSettingFilePath
            .Select(x => Path.GetFileName(x))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(this.CompositeDisposable);
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
