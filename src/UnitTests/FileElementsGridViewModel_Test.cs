﻿using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Threading;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Schedulers;

namespace UnitTests;

public class FileElementsGridViewModel_Test : IClassFixture<LogFixture>
{
    private const string targetDirPath = @"D:\FileRenamerDiff_Test";
    private const string SubDirName = "D_SubDir";
    private static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
    private static readonly string filePathB = Path.Combine(targetDirPath, "B.txt");
    private static readonly string filePathC = Path.Combine(targetDirPath, "C.txt");
    private static readonly string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
    private static readonly string filePathE = Path.Combine(targetDirPath, SubDirName, "E.txt");
    private static readonly string filePathF = Path.Combine(targetDirPath, SubDirName, "F.txt");

    private static MockFileSystem CreateMockFileSystem()
    {
        return new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [SettingAppModel.DefaultFilePath] = new MockFileData(string.Empty),
            [filePathA] = new MockFileData("A"),
            [filePathB] = new MockFileData("B"),
            [filePathC] = new MockFileData("C"),
            [filePathDSubDir] = new MockDirectoryData(),
            [filePathE] = new MockFileData("E"),
            [filePathF] = new MockFileData("F"),
        });
    }

    private static MainModel CreateDefaultSettingModel()
    {
        MockFileSystem fileSystem = CreateMockFileSystem();

        var model = new MainModel(fileSystem, Scheduler.Immediate);
        model.Initialize();
        model.Setting.SearchFilePaths = new[] { targetDirPath };
        return model;
    }

    [WpfFact]
    public async Task Counts_NoFile()
    {
        ReactivePropertyScheduler.SetDefault(new ReactivePropertyWpfScheduler(Dispatcher.CurrentDispatcher));
        var model = CreateDefaultSettingModel();

        await model.LoadFileElements();
        var fileElementVMs = new FileElementsGridViewModel(model);

        //ステージ 置換前
        fileElementVMs.IsAnyFiles.Value
            .Should().BeTrue("ファイル読込後なので、ファイルはあるはず");

        var cViewFileElementVMs = (ListCollectionView)fileElementVMs.CViewFileElementVMs;
        cViewFileElementVMs.IsEmpty
            .Should().BeFalse("ファイル読込後なので、ファイルはあるはず");
        cViewFileElementVMs.Count
            .Should().Be(6, "ファイル読込後なので、ファイルはあるはず");

        fileElementVMs.CountReplaced.Value
            .Should().Be(0, "置換前はまだ0のはず");

        fileElementVMs.CountConflicted.Value
            .Should().Be(0, "置換前はまだ0のはず");

        //ステージ なにも置換しない置換後
        await model.Replace();

        await model.WaitIdleUI().Timeout(3000);

        fileElementVMs.CountReplaced.Value
            .Should().Be(0, "置換する設定がないので、0のはず");
        fileElementVMs.CountConflicted.Value
            .Should().Be(0, "置換する設定がないので、0のはず");

        fileElementVMs.IsReplacedAny.Value
            .Should().BeFalse("置換する設定がないので、0のはず");
        fileElementVMs.IsNotConflictedAny.Value
            .Should().BeTrue("置換する設定がないので、0のはず");
    }

    [WpfFact]
    public async Task Counts_NotConflict()
    {
        var model = CreateDefaultSettingModel();

        await model.LoadFileElements();
        var fileElementVMs = new FileElementsGridViewModel(model);

        var cViewFileElementVMs = (ListCollectionView)fileElementVMs.CViewFileElementVMs;

        model.Setting.ReplaceTexts.Add(new("B", "BBB"));
        model.Setting.ReplaceTexts.Add(new("C", "CCC"));

        //ステージ 衝突はしない置換後
        await model.Replace();

        await model.WaitIdleUI().Timeout(3000);

        await fileElementVMs.CountReplaced
            .WaitShouldBe(2, 3000d, "B→BBB・C→CCCの置換する設定があるので");
        await fileElementVMs.CountConflicted
            .WaitShouldBe(0, 3000d, "衝突はしないので");

        fileElementVMs.IsReplacedAny.Value
            .Should().BeTrue("置換する設定があるので");
        fileElementVMs.IsNotConflictedAny.Value
            .Should().BeTrue("衝突はしないので");

        //ステージ 置換ファイルのみ表示にした後
        fileElementVMs.IsVisibleReplacedOnly.Value = true;

        await cViewFileElementVMs.ObserveProperty(x => x.Count)
            .WaitShouldBe(2, 3000d, "フィルタ後は絞られたはず");

        //ステージ すべてのファイル表示にした後
        fileElementVMs.IsVisibleConflictedOnly.Value = false;
        fileElementVMs.IsVisibleReplacedOnly.Value = false;

        await cViewFileElementVMs.ObserveProperty(x => x.Count)
            .WaitShouldBe(6, 3000d, "フィルタ削除後はすべてのファイルがあるはず");
    }

    [WpfFact]
    public async Task Counts_Conflict()
    {
        var model = CreateDefaultSettingModel();

        await model.LoadFileElements();
        var fileElementVMs = new FileElementsGridViewModel(model);
        await Task.Delay(10);

        fileElementVMs.IsAnyFiles.Value
            .Should().BeTrue("ファイル読込後なので、ファイルはあるはず");

        ListCollectionView cViewFileElementVMs = (ListCollectionView)fileElementVMs.CViewFileElementVMs;

        //ステージ 衝突する置換後
        const string conflictText = "A";
        model.Setting.ReplaceTexts.Add(new("B", conflictText));
        model.Setting.ReplaceTexts.Add(new("C", conflictText));

        await model.Replace();

        await model.WaitIdleUI().Timeout(3000);

        await fileElementVMs.CountReplaced
            .WaitShouldBe(2, 3000d, "B→A・C→Aの2つの置換する設定があるので");
        await fileElementVMs.CountConflicted
            .WaitShouldBe(3, 3000d, "元々A・B→A・C→Aの3つが衝突するので");

        fileElementVMs.IsReplacedAny.Value
            .Should().BeTrue("置換する設定があるので");
        fileElementVMs.IsNotConflictedAny.Value
            .Should().BeFalse("衝突するので");

        foreach (FileElementViewModel vm in cViewFileElementVMs)
        {
            (vm.PathModel.OutputFileName.Contains(conflictText) == vm.IsConflicted.Value)
                .Should().BeTrue("Aを含んだファイル名は衝突しているはず");
        }

        //ステージ 衝突ファイルのみ表示にした後
        fileElementVMs.IsVisibleConflictedOnly.Value = true;

        await cViewFileElementVMs.ObserveProperty(x => x.Count)
            .WaitShouldBe(3, 3000d, "元々A・B→A・C→Aの3つのファイルにフィルタ後は絞られたはず");

        //ステージ 衝突＆置換ファイルのみ表示にした後
        fileElementVMs.IsVisibleReplacedOnly.Value = true;

        await cViewFileElementVMs.ObserveProperty(x => x.Count)
            .WaitShouldBe(2, 3000d, "B→A・C→Aの2つのファイルにフィルタ後は絞られたはず");

        //ステージ すべてのファイル表示にした後
        fileElementVMs.IsVisibleConflictedOnly.Value = false;
        fileElementVMs.IsVisibleReplacedOnly.Value = false;

        await cViewFileElementVMs.ObserveProperty(x => x.Count)
            .WaitShouldBe(6, 3000d, "フィルタ削除後はすべてのファイルがあるはず");
    }
}
