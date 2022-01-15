using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Reactive.Bindings;

namespace UnitTests;

public class MainModel_Replace
{
    private const string topDirName = "FileRenamerDiff_Test";
    private const string targetDirPath = $@"D:\{topDirName}";
    private const string SubDirName = "D_SubDir";
    private const string lastWriteTimeText = "2020-01-23";
    private static readonly DateTime lastWriteTime = new(2020, 1, 23, 16, 7, 55, DateTimeKind.Utc);
    private const string creationTimeText = "2019-08-07";
    private static readonly DateTime creationTime = new(2019, 8, 7, 16, 55, 43, DateTimeKind.Utc);

    private static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
    private static readonly string filePathB = Path.Combine(targetDirPath, "B.txt");
    private static readonly string filePathC = Path.Combine(targetDirPath, "C.txt");
    private static readonly string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
    private static readonly string filePathE = Path.Combine(targetDirPath, SubDirName, "sam [p] [le].txt");
    private static readonly string filePathF = Path.Combine(targetDirPath, SubDirName, "saXmXple.txt");

    private static MockFileSystem CreateMockFileSystem()
    {
        var files = new (string path, MockFileData file)[]
            {
                (filePathA , new ("A")),
                (filePathB , new ("B")),
                (filePathC , new ("C")),
                (filePathDSubDir , new MockDirectoryData()),
                (filePathE , new ("E")),
                (filePathF , new ("F")),
            }
            .Do(x => x.file.LastWriteTime = lastWriteTime)
            .Do(x => x.file.CreationTime = creationTime)
            .ToDictionary(
               keySelector: x => x.path,
               elementSelector: x => x.file);

        return new MockFileSystem(files);
    }

    private static MainModel CreateDefaultSettingModel()
    {
        MockFileSystem fileSystem = CreateMockFileSystem();

        var model = new MainModel(fileSystem, Scheduler.Immediate);
        model.Initialize();
        model.Setting.SearchFilePaths = new[] { targetDirPath };
        return model;
    }

    [Fact]
    public async Task DeleteComplex()
    {
        MainModel model = CreateDefaultSettingModel();

        await model.LoadFileElements();

        model.Setting.DeleteTexts.Add(new(@"\[.*\]", "", true));
        model.Setting.DeleteTexts.Add(new(@"\s+(?=\.)", "", true));

        model.Setting.DeleteTexts.Add(new(@"X", ""));
        model.Setting.ReplaceTexts.Add(new(@"amp", "AMP"));

        await model.Replace();

        model.FileElementModels[1].OutputFileName
            .Should().Be("sam.txt");

        model.FileElementModels[0].OutputFileName
            .Should().Be("sAMPle.txt");
    }

    [Fact]
    public async Task AddDirectoryNameSetting()
    {
        MainModel model = CreateDefaultSettingModel();

        await model.LoadFileElements();

        model.Setting.IsDirectoryRenameTarget = false;

        model.Setting.ReplaceTexts.Add(new("^", "$d_", true));

        await model.Replace();

        model.FileElementModels.Select(x => x.OutputFileName)
            .Should().BeEquivalentTo(
            new[]
            {
                    $"{SubDirName}_saXmXple.txt",
                    $"{SubDirName}_sam [p] [le].txt",
                    $"{topDirName}_{SubDirName}",
                    $"{topDirName}_C.txt",
                    $"{topDirName}_B.txt",
                    $"{topDirName}_A.txt",
            });
    }

    [Fact]
    public async Task AddSerialNumberSetting()
    {
        MainModel model = CreateDefaultSettingModel();

        await model.LoadFileElements();

        model.Setting.IsDirectoryRenameTarget = false;

        model.Setting.ReplaceTexts.Add(new("^", "$n<50,10,000,r>_", true));

        await model.Replace();

        model.FileElementModels.Select(x => x.OutputFileName)
            .Should().BeEquivalentTo(
            new[]
            {
                    $"060_saXmXple.txt",
                    $"050_sam [p] [le].txt",
                    $"080_{SubDirName}",
                    $"070_C.txt",
                    $"060_B.txt",
                    $"050_A.txt",
            });
    }

    [Fact]
    public async Task AddLastWriteTimeSetting()
    {
        MainModel model = CreateDefaultSettingModel();

        await model.LoadFileElements();

        model.Setting.IsDirectoryRenameTarget = false;

        model.Setting.ReplaceTexts.Add(new("^", "$t_", true));

        await model.Replace();

        model.FileElementModels.Select(x => x.OutputFileName)
            .Should().BeEquivalentTo(
            new[]
            {
                    $"{lastWriteTimeText}_saXmXple.txt",
                    $"{lastWriteTimeText}_sam [p] [le].txt",
                    $"{lastWriteTimeText}_{SubDirName}",
                    $"{lastWriteTimeText}_C.txt",
                    $"{lastWriteTimeText}_B.txt",
                    $"{lastWriteTimeText}_A.txt",
            });
    }

    [Fact]
    public async Task AddCreationTimeSetting()
    {
        MainModel model = CreateDefaultSettingModel();

        await model.LoadFileElements();

        model.Setting.IsDirectoryRenameTarget = false;

        model.Setting.ReplaceTexts.Add(new("^", "$t<,c>_", true));

        await model.Replace();

        model.FileElementModels.Select(x => x.OutputFileName)
            .Should().BeEquivalentTo(
            new[]
            {
                    $"{creationTimeText}_saXmXple.txt",
                    $"{creationTimeText}_sam [p] [le].txt",
                    $"{creationTimeText}_{SubDirName}",
                    $"{creationTimeText}_C.txt",
                    $"{creationTimeText}_B.txt",
                    $"{creationTimeText}_A.txt",
            });
    }
}
