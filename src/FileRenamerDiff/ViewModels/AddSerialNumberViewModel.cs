using System.IO.Abstractions;
using System.Reactive.Linq;

using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;

using Reactive.Bindings;

namespace FileRenamerDiff.ViewModels;

public class AddSerialNumberViewModel
{
    private readonly MainModel mainModel;

    public ReactivePropertySlim<int> StartNumber { get; } = new(AddSerialNumberRegex.DefaultStartNumber);
    public ReactivePropertySlim<int> Step { get; } = new(AddSerialNumberRegex.DefaultStep);
    public ReactivePropertySlim<int> ZeroPadCount { get; } = new(1);
    public ReactivePropertySlim<bool> IsDirectoryReset { get; } = new();
    public ReactivePropertySlim<bool> IsInverseOrder { get; } = new();

    public ReadOnlyReactivePropertySlim<string> TextAddPattern { get; }

    public ReactivePropertySlim<string> TextTargetPattern { get; } = new("^");

    /// <summary>
    /// リネーム前後の差分比較情報
    /// </summary>
    public ReadOnlyReactivePropertySlim<IReadOnlyList<AddSerialNumberSampleViewModel>> SampleDiffVMs { get; }

    private static readonly IReadOnlyList<string> inputFilePaths = new[]
        {
            @"C:\Dir1\aaa.txt",
            @"C:\Dir1\bbb.txt",
            @"C:\Dir1\ccc.txt",
            @"C:\Dir2\ddd.txt",
        };

    private static readonly IReadOnlyList<IFileInfo> inputFileInfos = CreateFileInfos(inputFilePaths);

    private static IFileInfo[] CreateFileInfos(IReadOnlyList<string> inputFilePaths)
    {
        var fileSystem = AppExtension.CreateMockFileSystem(inputFilePaths);

        return inputFilePaths
            .Select(x => fileSystem.FileInfo.FromFileName(x))
            .ToArray();
    }

    /// <summary>
    /// 現在の設定のパターンへの追加
    /// </summary>
    public ReactiveCommand AddSettingCommand { get; }

    /// <summary>
    /// デザイナー用です　コードからは呼べません
    /// </summary>
    [Obsolete("Designer only", true)]
    public AddSerialNumberViewModel() : this(DesignerModel.MainModelForDesigner) { }
    public AddSerialNumberViewModel(MainModel mainModel)
    {
        this.mainModel = mainModel;
        this.TextAddPattern = Observable
            .CombineLatest(StartNumber, Step, ZeroPadCount, IsDirectoryReset, IsInverseOrder,
                (start, step, pad, isReset, isInverse) =>
                    CreatePatternText(start, step, pad, isReset, isInverse))
            .ToReadOnlyReactivePropertySlim("$n");

        this.SampleDiffVMs = Observable
            .CombineLatest(TextTargetPattern, TextAddPattern,
                (target, addPattern) => new { target, addPattern })
            .Select(a => new AddSerialNumberRegex(new Regex(a.target), a.addPattern))
            .Select(regex =>
                inputFileInfos
                    .Select(fsInfo => CreateSampleViewModel(fsInfo, regex))
                    .ToArray())
            .ToReadOnlyReactivePropertySlim<IReadOnlyList<AddSerialNumberSampleViewModel>>();

        this.AddSettingCommand = Observable
            .CombineLatest(TextTargetPattern, TextAddPattern,
                (x, y) => x.HasText() && y.HasText())
            .ToReactiveCommand()
            .WithSubscribe(() =>
                this.mainModel.Setting.ReplaceTexts
                    .Add(new ReplacePattern(TextTargetPattern.Value, TextAddPattern.Value, true)));
    }

    private static AddSerialNumberSampleViewModel CreateSampleViewModel(IFileInfo fsInfo, AddSerialNumberRegex regex)
    {
        string inputFileName = fsInfo.Name;
        string outputFileName = regex.Replace(inputFileName, inputFilePaths, fsInfo);
        return new(
            SampleDiff: AppExtension.CreateDiff(inputFileName, outputFileName),
            DirectoryPath: fsInfo.DirectoryName);
    }

    private static readonly Regex regexTailDefaultParamerters = new(",+(?=>)", RegexOptions.Compiled);
    private static readonly Regex regexAllDefaultParamerters = new("<,*>", RegexOptions.Compiled);

    private static string CreatePatternText(int start, int step, int pad, bool isReset, bool isInverse)
    {
        //各パラメータが初期値だったら空白にする
        string startText = start == AddSerialNumberRegex.DefaultStartNumber ? string.Empty : start.ToString();
        string stepText = step == AddSerialNumberRegex.DefaultStep ? string.Empty : step.ToString();
        string padText = pad <= 1 ? string.Empty : new string('0', pad);
        string isResetText = isReset == AddSerialNumberRegex.DefaultIsDirectoryReset
            ? string.Empty
            : AddSerialNumberRegex.DirectoryResetText;
        string isInverseText = isInverse == AddSerialNumberRegex.DefaultIsInverseOrder
            ? string.Empty
            : AddSerialNumberRegex.InverseOrderText;

        string paramerterText = @$"$n<{startText},{stepText},{padText},{isResetText},{isInverseText}>";
        //不要なパラメータを削除
        paramerterText = regexTailDefaultParamerters.Replace(paramerterText, string.Empty);
        //全部デフォルトパラメータだったら、削除
        paramerterText = regexAllDefaultParamerters.Replace(paramerterText, string.Empty);

        return paramerterText;
    }

    public record AddSerialNumberSampleViewModel(SideBySideDiffModel SampleDiff, string DirectoryPath);
}
