﻿using System.IO.Abstractions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

using Moq;

using Reactive.Bindings;

namespace UnitTests;

public class MainModel_LoadFiles
{
    private const string targetDirPath = @"D:\FileRenamerDiff_Test";
    private const string SubDirName = "D_SubDir";
    private static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
    private static readonly string filePathB = Path.Combine(targetDirPath, "B.csv");
    private static readonly string filePathCini = Path.Combine(targetDirPath, "C.ini");
    private static readonly string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
    private static readonly string filePathE = Path.Combine(targetDirPath, SubDirName, "E.txt");
    private static readonly string filePathFHidden = Path.Combine(targetDirPath, SubDirName, "F_Hidden.txt");
    private static readonly string filePathGSubSubDir = Path.Combine(targetDirPath, SubDirName, "G_SubSub");
    private static readonly string filePathHSubHiddenDir = Path.Combine(targetDirPath, "H_SubHidden");

    private static MockFileSystem CreateMockFileSystem()
    {
        return new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            [filePathA] = new MockFileData("A"),
            [filePathB] = new MockFileData("B"),
            [filePathCini] = new MockFileData("C"),
            [filePathDSubDir] = new MockDirectoryData(),
            [filePathE] = new MockFileData("E"),
            [filePathFHidden] = new MockFileData("F") { Attributes = FileAttributes.Hidden },
            [filePathGSubSubDir] = new MockDirectoryData(),
            [filePathHSubHiddenDir] = new MockDirectoryData() { Attributes = FileAttributes.Directory | FileAttributes.Hidden },
        });
    }

    private static MainModel CreateDefaultSettingModel(IFileSystem? fileSystem = null)
    {
        fileSystem ??= CreateMockFileSystem();

        var model = new MainModel(fileSystem, Scheduler.Immediate);
        model.Initialize();
        model.Setting.SearchFilePaths = new[] { targetDirPath };
        return model;
    }

    [Fact]
    public async Task LoadFile_DefaultSetting_IgnoreIniHidden()
    {
        MainModel model = CreateDefaultSettingModel();

        await model.LoadFileElements();

        model.FileElementModels
            .Select(f => f.InputFilePath)
            .Should().BeEquivalentTo(
                new[] { filePathA, filePathB, filePathDSubDir, filePathE, filePathGSubSubDir },
                "iniファイルを除いた、トップ階層のファイルのみが列挙されるはず");
    }

    [Fact]
    public async Task LoadFile_All()
    {
        MainModel model = CreateDefaultSettingModel();

        model.Setting.IgnoreExtensions.Clear();
        model.Setting.IsFileRenameTarget = true;
        model.Setting.IsDirectoryRenameTarget = true;
        model.Setting.IsHiddenRenameTarget = true;
        model.Setting.IsSearchSubDirectories = true;

        await model.LoadFileElements();

        model.FileElementModels
            .Select(f => f.InputFilePath)
            .Should().BeEquivalentTo(
                new[] { filePathA, filePathB, filePathCini, filePathDSubDir, filePathE, filePathFHidden, filePathGSubSubDir, filePathHSubHiddenDir },
                "すべてのファイル・フォルダが列挙されるはず");
    }

    [Fact]
    public async Task LoadFile_OnlyDir()
    {
        MainModel model = CreateDefaultSettingModel();

        model.Setting.IgnoreExtensions.Clear();
        model.Setting.IsFileRenameTarget = false;
        model.Setting.IsDirectoryRenameTarget = true;
        model.Setting.IsHiddenRenameTarget = true;
        model.Setting.IsSearchSubDirectories = true;

        await model.LoadFileElements();

        model.FileElementModels
            .Select(f => f.InputFilePath)
            .Should().BeEquivalentTo(
                new[] { filePathDSubDir, filePathGSubSubDir, filePathHSubHiddenDir },
                "すべてのフォルダが列挙されるはず");
    }

    [Fact]
    public async Task LoadFile_OnlyFile()
    {
        MainModel model = CreateDefaultSettingModel();

        model.Setting.IgnoreExtensions.Clear();
        model.Setting.IsFileRenameTarget = true;
        model.Setting.IsDirectoryRenameTarget = false;
        model.Setting.IsHiddenRenameTarget = true;
        model.Setting.IsSearchSubDirectories = true;

        await model.LoadFileElements();

        model.FileElementModels
            .Select(f => f.InputFilePath)
            .Should().BeEquivalentTo(
                new[] { filePathA, filePathB, filePathCini, filePathE, filePathFHidden, },
                "すべてのファイルが列挙されるはず");
    }

    [Fact]
    public async Task LoadFile_OnlyTopIgnoreHidden()
    {
        MainModel model = CreateDefaultSettingModel();

        model.Setting.IgnoreExtensions.Clear();
        model.Setting.IsFileRenameTarget = true;
        model.Setting.IsDirectoryRenameTarget = true;
        model.Setting.IsHiddenRenameTarget = false;
        model.Setting.IsSearchSubDirectories = false;

        await model.LoadFileElements();

        model.FileElementModels
            .Select(f => f.InputFilePath)
            .Should().BeEquivalentTo(
                new[] { filePathA, filePathB, filePathCini, filePathDSubDir },
                "トップ階層の隠しファイル以外のファイル・フォルダが列挙されるはず");
    }

    [Fact]
    public async Task LoadFile_Frequentory()
    {
        MainModel model = CreateDefaultSettingModel();

        for (int i = 0; i < 10; i++)
        {
            await model.LoadFileElements();
            model.FileElementModels.Count
                .Should().Be(5);

            await Task.Delay(50);
        }
    }

    [Fact]
    public async Task LoadFile_Nothing()
    {
        MainModel model = CreateDefaultSettingModel();

        //すべてのファイルを無視するような設定にする
        model.Setting.IgnoreExtensions.Add(new("txt"));
        model.Setting.IgnoreExtensions.Add(new("csv"));

        model.Setting.IsDirectoryRenameTarget = false;
        model.Setting.IsSearchSubDirectories = false;

        Task<AppMessage> taskMessage = model.MessageEvent.FirstAsync().ToTask();

        //ファイル読み込むが、無視されるファイルしか無い
        await model.LoadFileElements();

        model.FileElementModels
            .Should().BeEmpty("ファイルがなにもないはず");

        (await taskMessage).MessageHead
            .Should().Be("NOT FOUND");
    }

    [Fact]
    public async Task LoadFile_NoFileTarget()
    {
        MainModel model = CreateDefaultSettingModel();

        //すべてのファイルを無視するような設定にする
        model.Setting.IsDirectoryRenameTarget = false;
        model.Setting.IsFileRenameTarget = false;

        Task<AppMessage> taskMessage = model.MessageEvent.FirstAsync().ToTask();

        //ファイル読み込むが、無視されるファイルしか無い
        await model.LoadFileElements();

        model.FileElementModels
            .Should().BeEmpty("ファイルがなにもないはず");

        (await taskMessage).MessageHead
            .Should().Be("NOT FOUND");
    }

    [Fact]
    public async Task LoadFile_MannyFiles()
    {
        var files = Enumerable.Range(0, 10000)
            .Select(i => $"ManyFile-{i:0000}.txt")
            .Select(x => Path.Combine(targetDirPath, x));

        MainModel model = CreateDefaultSettingModel(AppExtension.CreateMockFileSystem(files));

        var progressInfos = model.CurrentProgressInfo
            .Skip(1)
            .WhereNotNull()
            .ToReadOnlyList();

        progressInfos
            .Should().BeEmpty("まだメッセージがないはず");

        //たくさんのファイルを読み込む
        await model.LoadFileElements();

        model.FileElementModels
            .Select(f => f.InputFilePath)
                .Should().HaveCount(10000, "規定数ファイルが読み込まれたはず");

        progressInfos.Select(x => x!.Message).Where(x => x.Contains("ManyFile"))
            .Should().HaveCountGreaterThan(2, "ファイル名を含んだメッセージがいくつか来たはず");
    }

    [Fact]
    public async Task LoadFile_Cancel()
    {
        var moqFileSystem = new Mock<IFileSystem>();
        moqFileSystem
            .Setup(x => x.File.Exists(It.Is<string>(s => s.Contains(targetDirPath)))).Returns(true);
        moqFileSystem
            .Setup(x => x.Directory.Exists(It.Is<string>(s => s.Contains(targetDirPath)))).Returns(true);

        moqFileSystem
            .Setup(x => x.File.Exists(It.IsAny<string>())).Returns(false);

        moqFileSystem
            .Setup(x => x.Directory.EnumerateFileSystemEntries(It.IsAny<string>(), "*", It.IsAny<EnumerationOptions>()))
            .Returns(Enumerable
                .Range(0, 100000)
                .Select(i => $"ManyFile-{i:00000}.txt")
                .Select(x => Path.Combine(targetDirPath, x)));

        //たくさんのファイルの読込を開始する
        MainModel model = CreateDefaultSettingModel(moqFileSystem.Object);

        var firstProgressTask = model.CurrentProgressInfo
            .WhereNotNull().FirstAsync().ToTask();

        Task loadTask = model.LoadFileElements();

        //255個ファイルを読み込まれた時点で、ProgressInfoが１つくる
        await firstProgressTask.Timeout(3000);

        model.CancelWork?.Cancel();

        await loadTask.Timeout(3000d);

        model.FileElementModels
            .Should().BeEmpty("途中まで読み込んだファイルもクリアされる");

        model.CurrentProgressInfo.Value?.Message
            .Should().Contain("cancel", because: "キャンセルメッセージがきたはず");
    }
}
