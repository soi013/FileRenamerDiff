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

using FileRenamerDiff.Models;
using FileRenamerDiff.Properties;
using Reactive.Bindings.ObjectExtensions;
using Anotar.Serilog;

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// 設定情報を編集するViewModel
    /// </summary>
    public class SettingAppViewModel : ViewModel
    {
        private Model model = Model.Instance;
        private SettingAppModel setting;

        /// <summary>
        /// リネームファイルを検索するターゲットパス
        /// </summary>
        public ReactivePropertySlim<string> SearchFilePath => setting.SearchFilePath;

        /// <summary>
        /// 検索時に無視される拡張子コレクション
        /// </summary>
        public ObservableCollection<ReactivePropertySlim<string>> IgnoreExtensions => setting.IgnoreExtensions;

        /// <summary>
        /// 削除文字列パターン
        /// </summary>
        public ObservableCollection<ReplacePattern> DeleteTexts => setting.DeleteTexts;

        /// <summary>
        /// よく使う削除パターン集
        /// </summary>
        public IReadOnlyList<CommonPatternViewModel> CommonDeletePatternVMs { get; }
            = CommonPattern.DeletePatterns.Select(x => new CommonPatternViewModel(x, true)).ToArray();

        /// <summary>
        /// 置換文字列パターン
        /// </summary>
        public ObservableCollection<ReplacePattern> ReplaceTexts => setting.ReplaceTexts;

        /// <summary>
        /// よく使う置換パターン集
        /// </summary>
        public IReadOnlyList<CommonPatternViewModel> CommonReplacePatternVMs { get; }
            = CommonPattern.ReplacePatterns.Select(x => new CommonPatternViewModel(x, false)).ToArray();

        /// <summary>
        /// ファイル探索時にサブディレクトリを探索するか
        /// </summary>
        public ReactivePropertySlim<bool> IsSearchSubDirectories => setting.IsSearchSubDirectories;

        /// <summary>
        /// ディレクトリをリネーム対象にするか
        /// </summary>
        public ReactivePropertySlim<bool> IsDirectoryRenameTarget => setting.IsDirectoryRenameTarget;

        /// <summary>
        /// ディレクトリでないファイルをリネーム対象にするか
        /// </summary>
        public ReactivePropertySlim<bool> IsFileRenameTarget => setting.IsFileRenameTarget;

        /// <summary>
        /// 隠しファイルをリネーム対象にするか
        /// </summary>
        public ReactivePropertySlim<bool> IsHiddenRenameTarget => setting.IsHiddenRenameTarget;

        /// <summary>
        /// 選択可能な言語一覧
        /// </summary>
        public IReadOnlyList<CultureInfo> AvailableLanguages { get; }

        /// <summary>
        /// アプリケーションの表示言語
        /// </summary>
        public ReactivePropertySlim<CultureInfo> SelectedLanguage { get; } = new ReactivePropertySlim<CultureInfo>();

        /// <summary>
        /// アプリケーションの色テーマ
        /// </summary>
        public ReactivePropertySlim<bool> IsAppDarkTheme => setting.IsAppDarkTheme;

        public ReactiveCommand AddIgnoreExtensionsCommand { get; }

        public AsyncReactiveCommand ClearIgnoreExtensionsCommand { get; }

        public ReactiveCommand AddDeleteTextsCommand { get; }
        public AsyncReactiveCommand ClearDeleteTextsCommand { get; }
        public ReactiveCommand AddReplaceTextsCommand { get; }
        public AsyncReactiveCommand ClearReplaceTextsCommand { get; }

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
        public SettingAppViewModel() : this(new SettingAppModel()) { }

        public SettingAppViewModel(SettingAppModel setting)
        {
            this.setting = setting;

            this.AvailableLanguages = CreateAvailableLanguages();
            this.SelectedLanguage = CreateAppLanguageRp();

            this.AddIgnoreExtensionsCommand = model.IsIdleUI
                 .ToReactiveCommand()
                 .WithSubscribe(() => setting.AddIgnoreExtensions())
                 .AddTo(this.CompositeDisposable);

            this.ClearIgnoreExtensionsCommand =
                new[]
                {
                    model.IsIdleUI,
                    setting.IgnoreExtensions.ObserveIsAny(),
                }
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() =>
                    model.ExcuteAfterConfirm(() =>
                        setting.IgnoreExtensions.Clear()))
                 .AddTo(this.CompositeDisposable);

            this.AddDeleteTextsCommand = model.IsIdleUI
                 .ToReactiveCommand()
                 .WithSubscribe(() => setting.AddDeleteTexts())
                 .AddTo(this.CompositeDisposable);
            this.ClearDeleteTextsCommand =
                new[]
                {
                    model.IsIdleUI,
                    setting.DeleteTexts.ObserveIsAny(),
                }
                .CombineLatestValuesAreAllTrue()
                 .ToAsyncReactiveCommand()
                 .WithSubscribe(() =>
                    model.ExcuteAfterConfirm(() =>
                        setting.DeleteTexts.Clear()))
                 .AddTo(this.CompositeDisposable);

            this.AddReplaceTextsCommand = model.IsIdleUI
                .ToReactiveCommand()
                .WithSubscribe(() => setting.AddReplaceTexts())
                .AddTo(this.CompositeDisposable);
            this.ClearReplaceTextsCommand =
                new[]
                {
                    model.IsIdleUI,
                    setting.ReplaceTexts.ObserveIsAny(),
                }
                .CombineLatestValuesAreAllTrue()
                 .ToAsyncReactiveCommand()
                 .WithSubscribe(() =>
                    model.ExcuteAfterConfirm(() =>
                        setting.ReplaceTexts.Clear()))
                 .AddTo(this.CompositeDisposable);

            this.ResetSettingCommand = model.IsIdleUI
                .ToReactiveCommand()
                .WithSubscribe(() => model.ResetSetting())
                .AddTo(this.CompositeDisposable);

            this.LoadSettingFileDialogCommand = model.IsIdleUI
                .ToReactiveCommand<FileSelectionMessage>()
                .WithSubscribe(x => model.LoadSettingFile(x?.Response?.FirstOrDefault()));

            this.SaveSettingFileDialogCommand = model.IsIdleUI
                .ToReactiveCommand<FileSelectionMessage>()
                .WithSubscribe(x => model.SaveSettingFile(x?.Response?.FirstOrDefault()));

            this.PreviousSettingFileDirectory = model.PreviousSettingFilePath
                .Select(x => Path.GetDirectoryName(x))
                .ToReadOnlyReactivePropertySlim();
            this.PreviousSettingFileName = model.PreviousSettingFilePath
                .Select(x => Path.GetFileName(x))
                .ToReadOnlyReactivePropertySlim();
        }

        private static CultureInfo[] CreateAvailableLanguages()
        {
            var resourceManager = new ResourceManager(typeof(Resources));
            return CultureInfo.GetCultures(CultureTypes.AllCultures)
                            .Where(x => !x.Equals(CultureInfo.InvariantCulture))
                            .Where(x => IsAvailableCulture(x, resourceManager))
                            .Concat(new[] { CultureInfo.GetCultureInfo("en"), CultureInfo.InvariantCulture })
                            .ToArray();
        }

        private static bool IsAvailableCulture(CultureInfo cultureInfo, ResourceManager resourceManager) =>
            resourceManager.GetResourceSet(cultureInfo, true, false) != null;

        private ReactivePropertySlim<CultureInfo> CreateAppLanguageRp()
        {
            //Model側から変更されることは無いはずなので、初期値のみ読込
            //リストにない言語の場合は、Autoに設定する
            var modelCultureInfo = CultureInfo.GetCultureInfo(setting.AppLanguageCode.Value);
            if (!this.AvailableLanguages.Contains(modelCultureInfo))
                modelCultureInfo = CultureInfo.InvariantCulture;

            var rp = new ReactivePropertySlim<CultureInfo>(modelCultureInfo);

            rp.Select(c =>
                    (c == null || c == CultureInfo.InvariantCulture)
                        ? ""
                        : c.Name)
                .Subscribe(c => setting.AppLanguageCode.Value = c);

            return rp;
        }
    }
}