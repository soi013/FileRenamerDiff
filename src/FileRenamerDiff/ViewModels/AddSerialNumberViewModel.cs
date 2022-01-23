using System.IO.Abstractions;
using System.Reactive.Linq;

using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;

using Reactive.Bindings;

namespace FileRenamerDiff.ViewModels;

/// <summary>
/// AddSerialNumberRegexの設定専用VM
/// </summary>
public class AddSerialNumberViewModel
{
    private readonly MainModel mainModel;

    /// <summary>
    /// 連番開始番号
    /// </summary>
    public ReactivePropertySlim<int> StartNumber { get; } = new(AddSerialNumberRegex.DefaultStartNumber);
    /// <summary>
    /// 連番ステップ数
    /// </summary>
    public ReactivePropertySlim<int> Step { get; } = new(AddSerialNumberRegex.DefaultStep);
    /// <summary>
    /// ゼロ埋め桁数
    /// </summary>
    public ReactivePropertySlim<int> ZeroPadCount { get; } = new(1);

    /// <summary>
    /// ディレクトリごと連番有効状態
    /// </summary>
    public ReactivePropertySlim<bool> IsDirectoryReset { get; } = new();
    /// <summary>
    /// 逆順有効状態
    /// </summary>
    public ReactivePropertySlim<bool> IsInverseOrder { get; } = new();

    /// <summary>
    /// 前置き固定文字
    /// </summary>
    public ReactivePropertySlim<string> PrefixText { get; } = new(string.Empty);
    /// <summary>
    /// 後置き固定文字
    /// </summary>
    public ReactivePropertySlim<string> PostfixText { get; } = new("_");

    /// <summary>
    /// 生成された文字列フォーマット
    /// </summary>
    public ReadOnlyReactivePropertySlim<string> TextAddPattern { get; }

    /// <summary>
    /// 置換対象となる文字列
    /// </summary>
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
            .CombineLatest(StartNumber, Step, ZeroPadCount, IsDirectoryReset, IsInverseOrder, PrefixText, PostfixText,
                (start, step, pad, isReset, isInverse, pre, post) =>
                    CreatePatternText(start, step, pad, isReset, isInverse, pre, post))
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

    /// <summary>
    /// 各種設定から連番追加の置換パラメータを生成する
    /// </summary>
    private static string CreatePatternText(int start, int step, int pad, bool isReset, bool isInverse, string pre, string post)
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

        string paramerterText = @$"{pre}$n<{startText},{stepText},{padText},{isResetText},{isInverseText}>{post}";
        //不要なパラメータを削除
        paramerterText = regexTailDefaultParamerters.Replace(paramerterText, string.Empty);
        //全部デフォルトパラメータだったら、削除
        paramerterText = regexAllDefaultParamerters.Replace(paramerterText, string.Empty);

        return paramerterText;
    }

    public record AddSerialNumberSampleViewModel(SideBySideDiffModel SampleDiff, string DirectoryPath);
}
