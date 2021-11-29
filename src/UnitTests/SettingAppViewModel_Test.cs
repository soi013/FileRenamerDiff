using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;

using Livet.Messaging.IO;

using Reactive.Bindings;

namespace UnitTests;

public class SettingAppViewModel_Test : IClassFixture<LogFixture>
{
    private const string targetDirPath = @"D:\FileRenamerDiff_Test";
    private const string targetDirPathSub = @"D:\FileRenamerDiff_TestSub";

    [WpfFact]
    public async Task Commands_CanExecute()
    {
        var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();
        await model.WaitIdleUI().Timeout(3000d);
        await Task.Delay(100);

        //ステージ1 初期状態
        var canExecuteUsuallyCommands = new ICommand[]
        {
                settingVM.AddDeleteTextsCommand,
                settingVM.AddIgnoreExtensionsCommand,
                settingVM.AddReplaceTextsCommand,
                settingVM.ClearDeleteTextsCommand,
                settingVM.ClearIgnoreExtensionsCommand,
                settingVM.ClearReplaceTextsCommand,
                settingVM.LoadSettingFileDialogCommand,
                settingVM.ResetSettingCommand,
                settingVM.SaveSettingFileDialogCommand,
                settingVM.ShowExpressionReferenceCommand,
        };

        foreach ((ICommand c, int i) in canExecuteUsuallyCommands.WithIndex())
        {
            c.CanExecute(null)
                .Should().BeTrue($"すべて実行可能はなず (indexCommand:{i})");
        }
    }

    [Fact]
    public void SearchFilePathConcate()
    {
        var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();

        IReadOnlyList<string> newPaths = new[] { targetDirPath, targetDirPathSub };
        settingVM.SearchFilePaths.Value = newPaths;
        settingVM.ConcatedSearchFilePaths.Value
            .Should().Contain(targetDirPath, "連結ファイルパスに元のファイルパスが含まれているはず")
            .And.Contain(targetDirPathSub, "連結ファイルパスに元のファイルパスが含まれているはず");

        settingVM.ConcatedSearchFilePaths.Value = $"{targetDirPath}|{targetDirPathSub}";
        settingVM.SearchFilePaths.Value
            .Should().Contain(targetDirPath, "連結ファイルパスに元のファイルパスが含まれているはず")
            .And.Contain(targetDirPathSub, "連結ファイルパスに元のファイルパスが含まれているはず");
    }

    [WpfFact]
    public void CommonDeletePattern()
    {
        var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();

        const string commonDeletePatternTarget = @".*";
        CommonPatternViewModel commonDeletePattern = settingVM.CommonDeletePatternVMs
                .Where(x => x.TargetPattern == commonDeletePatternTarget)
                .First();

        //ステージ1 追加前
        commonDeletePattern.TargetPattern
            .Should().Be(commonDeletePatternTarget);
        commonDeletePattern.ReplaceText
            .Should().BeEmpty();
        commonDeletePattern.AsExpression
            .Should().BeTrue();
        commonDeletePattern.SampleDiff.OldText.ToRawText()
            .Should().NotBeEmpty();
        commonDeletePattern.SampleDiff.NewText.ToRawText()
            .Should().BeEmpty();

        settingVM.DeleteTexts
            .Should().NotContain(x => x.TargetPattern.Value == commonDeletePatternTarget, "まだ含まれていないはず");

        //ステージ2 追加後
        commonDeletePattern.AddSettingCommand.Execute();
        settingVM.DeleteTexts
            .Should().Contain(x => x.TargetPattern.Value == commonDeletePatternTarget, "含まれているはず");

        //ステージ3 追加後編集
        ReplacePatternViewModel addedPattern = settingVM.DeleteTexts.Where(x => x.TargetPattern.Value == commonDeletePatternTarget).First();
        const string changedTarget = "XXX";
        addedPattern.TargetPattern.Value = changedTarget;

        settingVM.CommonDeletePatternVMs
            .Should().NotContain(x => x.TargetPattern == changedTarget, "追加先で編集しても、元のプロパティは変更されないはず")
            .And.Contain(x => x.TargetPattern == commonDeletePatternTarget, "追加先で編集しても、元のプロパティは変更されないはず");

        CommonPattern.DeletePatterns
            .Should().NotContain(x => x.TargetPattern == changedTarget, "追加先で編集しても、元のプロパティは変更されないはず")
            .And.Contain(x => x.TargetPattern == commonDeletePatternTarget, "追加先で編集しても、元のプロパティは変更されないはず");
    }

    [WpfFact]
    public void CommonReplacePattern()
    {
        var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();

        //英数小文字を大文字にするパターン(ex. low -> LOW)を取得
        const string commonReplacePatternTarget = "[a-z]";
        CommonPatternViewModel commonReplacePattern = settingVM.CommonReplacePatternVMs
                .Where(x => x.TargetPattern == commonReplacePatternTarget)
                .First();

        //ステージ1 追加前
        commonReplacePattern.TargetPattern
            .Should().Be(commonReplacePatternTarget);
        commonReplacePattern.ReplaceText
            .Should().Contain("\\u");
        commonReplacePattern.AsExpression
            .Should().BeTrue();
        commonReplacePattern.SampleDiff.OldText.ToRawText()
            .Should().Contain("low");
        commonReplacePattern.SampleDiff.NewText.ToRawText()
            .Should().Contain("LOW");

        settingVM.ReplaceTexts
            .Should().NotContain(x => x.TargetPattern.Value == commonReplacePatternTarget, "まだ含まれていないはず");

        //ステージ2 追加後
        commonReplacePattern.AddSettingCommand.Execute();
        settingVM.ReplaceTexts
            .Should().Contain(x => x.TargetPattern.Value == commonReplacePatternTarget, "含まれているはず");

        //ステージ3 追加後編集
        ReplacePatternViewModel addedPattern = settingVM.ReplaceTexts.Where(x => x.TargetPattern.Value == commonReplacePatternTarget).First();
        const string changedTarget = "XXX";
        addedPattern.TargetPattern.Value = changedTarget;

        settingVM.CommonReplacePatternVMs
            .Should().NotContain(x => x.TargetPattern == changedTarget, "追加先で編集しても、元のプロパティは変更されないはず")
            .And.Contain(x => x.TargetPattern == commonReplacePatternTarget, "追加先で編集しても、元のプロパティは変更されないはず");

        CommonPattern.ReplacePatterns
            .Should().NotContain(x => x.TargetPattern == changedTarget, "追加先で編集しても、元のプロパティは変更されないはず")
            .And.Contain(x => x.TargetPattern == commonReplacePatternTarget, "追加先で編集しても、元のプロパティは変更されないはず");
    }

    [WpfFact]
    public void OtherProperties()
    {
        var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();

        var defaultSetting = new SettingAppModel();

        //ステージ1 変更前
        settingVM.IsSearchSubDirectories.Value
            .Should().Be(defaultSetting.IsSearchSubDirectories, "デフォルト設定と同じはず");

        settingVM.IsDirectoryRenameTarget.Value
            .Should().Be(defaultSetting.IsDirectoryRenameTarget, "デフォルト設定と同じはず");

        settingVM.IsFileRenameTarget.Value
            .Should().Be(defaultSetting.IsFileRenameTarget, "デフォルト設定と同じはず");

        settingVM.IsHiddenRenameTarget.Value
            .Should().Be(defaultSetting.IsHiddenRenameTarget, "デフォルト設定と同じはず");

        settingVM.IsRenameExt.Value
            .Should().Be(defaultSetting.IsRenameExt, "デフォルト設定と同じはず");

        settingVM.AvailableLanguages
            .Should().BeEquivalentTo(SettingAppModel.AvailableLanguages, "Modelの静的プロパティと同じはず");
        settingVM.AvailableLanguages.Select(x => x.Name)
            .Should().BeEquivalentTo(
                new[] { string.Empty, "de", "en", "ja", "ru", "zh" },
                becauseArgs: "6個の言語があるはず");
        settingVM.SelectedLanguage.Value.Name
            .Should().Be(defaultSetting.AppLanguageCode, "デフォルト設定と同じはず");

        settingVM.IsAppDarkTheme.Value
            .Should().Be(defaultSetting.IsAppDarkTheme, "デフォルト設定と同じはず");

        settingVM.IsCreateRenameLog.Value
            .Should().Be(defaultSetting.IsCreateRenameLog, "デフォルト設定と同じはず");

        //ステージ2 変更後
        settingVM.IsSearchSubDirectories.Value ^= true;
        settingVM.IsDirectoryRenameTarget.Value ^= true;
        settingVM.IsFileRenameTarget.Value ^= true;
        settingVM.IsHiddenRenameTarget.Value ^= true;
        settingVM.IsRenameExt.Value ^= true;
        settingVM.IsAppDarkTheme.Value ^= true;
        settingVM.IsCreateRenameLog.Value ^= true;
        settingVM.SelectedLanguage.Value = settingVM.AvailableLanguages.First(x => x.Name.Contains("ja"));

        settingVM.IsSearchSubDirectories.Value
            .Should().Be(!defaultSetting.IsSearchSubDirectories, "デフォルト設定と違うはず");
        model.Setting.IsSearchSubDirectories
            .Should().Be(settingVM.IsSearchSubDirectories.Value, "VMとModelの値は同じはず");

        settingVM.IsDirectoryRenameTarget.Value
            .Should().Be(!defaultSetting.IsDirectoryRenameTarget, "デフォルト設定と違うはず");
        model.Setting.IsDirectoryRenameTarget
            .Should().Be(settingVM.IsDirectoryRenameTarget.Value, "VMとModelの値は同じはず");

        settingVM.IsFileRenameTarget.Value
            .Should().Be(!defaultSetting.IsFileRenameTarget, "デフォルト設定と違うはず");
        model.Setting.IsFileRenameTarget
            .Should().Be(settingVM.IsFileRenameTarget.Value, "VMとModelの値は同じはず");

        settingVM.IsHiddenRenameTarget.Value
            .Should().Be(!defaultSetting.IsHiddenRenameTarget, "デフォルト設定と違うはず");
        model.Setting.IsHiddenRenameTarget
            .Should().Be(settingVM.IsHiddenRenameTarget.Value, "VMとModelの値は同じはず");

        settingVM.IsRenameExt.Value
            .Should().Be(!defaultSetting.IsRenameExt, "デフォルト設定と違うはず");
        model.Setting.IsRenameExt
            .Should().Be(settingVM.IsRenameExt.Value, "VMとModelの値は同じはず");

        settingVM.IsAppDarkTheme.Value
            .Should().Be(!defaultSetting.IsAppDarkTheme, "デフォルト設定と違うはず");
        model.Setting.IsAppDarkTheme
            .Should().Be(settingVM.IsAppDarkTheme.Value, "VMとModelの値は同じはず");

        settingVM.SelectedLanguage.Value.Name
            .Should().NotBe(defaultSetting.AppLanguageCode, "デフォルト設定と違うはず");
        model.Setting.AppLanguageCode
            .Should().Be(settingVM.SelectedLanguage.Value.Name, "VMとModelの値は同じはず");

        settingVM.IsCreateRenameLog.Value
            .Should().Be(!defaultSetting.IsCreateRenameLog, "デフォルト設定と違うはず");
        model.Setting.IsCreateRenameLog
            .Should().Be(settingVM.IsCreateRenameLog.Value, "VMとModelの値は同じはず");
    }

    [WpfFact]
    public async Task Add_Clear_DeleteTextsCommand()
    {
        var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();
        await model.WaitIdleUI().Timeout(3000d);
        await Task.Delay(100);

        int defaultCount = model.Setting.DeleteTexts.Count;
        //ステージ1 追加後
        settingVM.AddDeleteTextsCommand.Execute();

        model.Setting.DeleteTexts
            .Should().HaveCount(defaultCount + 1, "1つ増えているはず");
        model.Setting.DeleteTexts.Last().ReplaceText
            .Should().BeEmpty("追加された内容は空白のはず");
        model.Setting.DeleteTexts.Last().TargetPattern
            .Should().BeEmpty("追加された内容は空白のはず");

        //ステージ2 クリア後
        await settingVM.ClearDeleteTextsCommand.ExecuteAsync();

        model.Setting.DeleteTexts
            .Should().BeEmpty("空になったはず");
    }

    [WpfFact]
    public async Task Add_Clear_IgnoreExtensionsCommand()
    {
        var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();
        await model.WaitIdleUI().Timeout(3000d);
        await Task.Delay(100);

        int defaultCount = model.Setting.IgnoreExtensions.Count;

        //ステージ1 追加後
        settingVM.AddIgnoreExtensionsCommand.Execute();

        model.Setting.IgnoreExtensions
            .Should().HaveCount(defaultCount + 1, "1つ増えているはず");
        model.Setting.IgnoreExtensions.Last().Value
            .Should().BeEmpty("追加された内容は空白のはず");

        //ステージ2 クリア後
        await settingVM.ClearIgnoreExtensionsCommand.ExecuteAsync();

        model.Setting.IgnoreExtensions
            .Should().BeEmpty("空になったはず");
    }

    [WpfFact]
    public async Task Add_Clear_ReplaceTextsCommand()
    {
        var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();
        await model.WaitIdleUI().Timeout(3000d);
        await Task.Delay(100);

        int defaultCount = model.Setting.ReplaceTexts.Count;

        //ステージ1 追加後
        settingVM.AddReplaceTextsCommand.Execute();

        model.Setting.ReplaceTexts
            .Should().HaveCount(defaultCount + 1, "1つ増えているはず");
        model.Setting.ReplaceTexts.Last().ReplaceText
            .Should().BeEmpty("追加された内容は空白のはず");
        model.Setting.ReplaceTexts.Last().TargetPattern
            .Should().BeEmpty("追加された内容は空白のはず");

        //ステージ2 クリア後
        await settingVM.ClearReplaceTextsCommand.ExecuteAsync();

        model.Setting.ReplaceTexts
            .Should().BeEmpty("空になったはず");
    }

    private static readonly string defaultSettingFilePath = SettingAppModel.DefaultFilePath;
    private static readonly string otherSettingFilePath = @"D:\FileRenamerDiff_Setting\Setting.json";

    [WpfFact]
    public void SaveSetting_Success()
    {
        var fileSystem = new MockFileSystem();
        var model = new MainModel(fileSystem, Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();

        const string newIgnoreExt = "someignoreext";
        settingVM.IgnoreExtensions.Add(new(newIgnoreExt));

        //ステージ1 保存前
        fileSystem.AllFiles
            .Should().BeEmpty();

        settingVM.PreviousSettingFileDirectory.Value
            .Should().Be(Path.GetDirectoryName(defaultSettingFilePath), "デフォルトファイルのディレクトリパスになっているはず");

        settingVM.PreviousSettingFileName.Value
            .Should().Be(Path.GetFileName(defaultSettingFilePath), "デフォルトファイル名前になっているはず");

        //ステージ2 保存後
        var saveMessage = new SavingFileSelectionMessage() { Response = new[] { otherSettingFilePath } };
        settingVM.SaveSettingFileDialogCommand.Execute(saveMessage);

        fileSystem.AllFiles
            .Should().BeEquivalentTo(new[] { otherSettingFilePath }, because: "設定ファイルが保存されているはず");

        fileSystem.File.ReadAllText(otherSettingFilePath)
            .Should().Contain(newIgnoreExt, because: "設定ファイルの中身に新しい設定値が保存されているはず");

        settingVM.PreviousSettingFileDirectory.Value
            .Should().Be(Path.GetDirectoryName(otherSettingFilePath), "保存したファイルのディレクトリパスになっているはず");

        settingVM.PreviousSettingFileName.Value
            .Should().Be(Path.GetFileName(otherSettingFilePath), "保存したファイル名前になっているはず");
    }


    [WpfFact]
    public void SaveSetting_NullFilePath()
    {
        var fileSystem = new MockFileSystem();
        var model = new MainModel(fileSystem, Scheduler.Immediate);
        var settingVM = new SettingAppViewModel(model);
        model.Initialize();

        const string newIgnoreExt = "someignoreext";
        settingVM.IgnoreExtensions.Add(new(newIgnoreExt));

        //ステージ2 保存後
        var saveMessage = new SavingFileSelectionMessage() { Response = null };
        settingVM.SaveSettingFileDialogCommand.Execute(saveMessage);

        fileSystem.AllFiles
            .Should().BeEmpty("保存できていないので、ファイルが増えていないはず");

        settingVM.PreviousSettingFileDirectory.Value
            .Should().Be(Path.GetDirectoryName(defaultSettingFilePath), "保存できていないので、デフォルトファイルのディレクトリパスになっているはず");

        settingVM.PreviousSettingFileName.Value
            .Should().Be(Path.GetFileName(defaultSettingFilePath), "保存できていないので、デフォルトファイル名前になっているはず");
    }

    [WpfFact]
    public async Task LoadSetting_Success()
    {
        const string firstIgnoreExt = "firstignoreext";
        const string otherIgnoreExt = "otherignoreext";

        string settingContent = @"{""IgnoreExtensions"":[{""Value"":""" + firstIgnoreExt + @"""}]}";
        string settingContentOther = @"{""IgnoreExtensions"":[{""Value"":""" + otherIgnoreExt + @"""}]}";

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [defaultSettingFilePath] = new MockFileData(settingContent),
            [otherSettingFilePath] = new MockFileData(settingContentOther),
        });

        var model = new MainModel(mockFileSystem, Scheduler.Immediate);
        var mainVM = new MainWindowViewModel(model);
        mainVM.Initialize();
        await model.WaitIdleUI().Timeout(3000d);
        await Task.Delay(100);

        //設定VMは設定読込時にリセットされるので、MainVMの設定プロパティからアクセスする
        //settingVM...

        //ステージ1 初期状態
        mainVM.SettingVM.Value.IgnoreExtensions.Select(x => x.Value)
                .Should().Contain(firstIgnoreExt, because: "起動時にファイルから設定値が読み込まれたはず");

        //ステージ2 読込後
        var fileMessage = new OpeningFileSelectionMessage() { Response = new[] { otherSettingFilePath } };
        mainVM.SettingVM.Value.LoadSettingFileDialogCommand.Execute(fileMessage);

        mainVM.SettingVM.Value.IgnoreExtensions.Select(x => x.Value)
            .Should().NotContain(firstIgnoreExt, because: "別の設定ファイルを読ませたら、元の設定値は消えたはず");

        mainVM.SettingVM.Value.IgnoreExtensions.Select(x => x.Value)
            .Should().Contain(otherIgnoreExt, because: "別の設定ファイルを読ませたら、ファイルから設定値が読み込まれたはず");

        mainVM.SettingVM.Value.PreviousSettingFileDirectory.Value
            .Should().Be(Path.GetDirectoryName(otherSettingFilePath), "保存したファイルのディレクトリパスになっているはず");

        mainVM.SettingVM.Value.PreviousSettingFileName.Value
            .Should().Be(Path.GetFileName(otherSettingFilePath), "保存したファイル名前になっているはず");
    }

    [WpfFact]
    public async Task LoadSetting_NullFilePath()
    {
        const string firstIgnoreExt = "firstignoreext";
        const string otherIgnoreExt = "otherignoreext";

        string settingContent = @"{""IgnoreExtensions"":[{""Value"":""" + firstIgnoreExt + @"""}]}";
        string settingContentOther = @"{""IgnoreExtensions"":[{""Value"":""" + otherIgnoreExt + @"""}]}";

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [defaultSettingFilePath] = new MockFileData(settingContent),
            [otherSettingFilePath] = new MockFileData(settingContentOther),
        });

        var model = new MainModel(mockFileSystem, Scheduler.Immediate);
        var mainVM = new MainWindowViewModel(model);
        mainVM.Initialize();
        await model.WaitIdleUI().Timeout(3000d);
        await Task.Delay(100);

        //設定VMは設定読込時にリセットされるので、MainVMの設定プロパティからアクセスする
        //settingVM...

        //ステージ1 初期状態
        mainVM.SettingVM.Value.IgnoreExtensions.Select(x => x.Value)
                .Should().Contain(firstIgnoreExt, because: "起動時にファイルから設定値が読み込まれたはず");

        //ステージ2 読込後
        var fileMessage = new OpeningFileSelectionMessage() { Response = null };
        mainVM.SettingVM.Value.LoadSettingFileDialogCommand.Execute(fileMessage);

        mainVM.SettingVM.Value.IgnoreExtensions.Select(x => x.Value)
            .Should().Contain(firstIgnoreExt, because: "設定を読み込めていないので、元の設定値のままのはず");

        mainVM.SettingVM.Value.IgnoreExtensions.Select(x => x.Value)
            .Should().NotContain(otherIgnoreExt, because: "設定を読み込めていないので、ファイルから設定値が読み込まれていないはず");

        mainVM.SettingVM.Value.PreviousSettingFileDirectory.Value
            .Should().Be(Path.GetDirectoryName(defaultSettingFilePath), "設定を読み込めていないので、デフォルトファイルのディレクトリパスになっているはず");

        mainVM.SettingVM.Value.PreviousSettingFileName.Value
            .Should().Be(Path.GetFileName(defaultSettingFilePath), "設定を読み込めていないので、デフォルトファイル名前になっているはず");
    }
}
