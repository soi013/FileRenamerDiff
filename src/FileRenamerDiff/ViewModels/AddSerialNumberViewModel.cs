using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Data;

using Anotar.Serilog;

using FileRenamerDiff.Models;

using Livet;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace FileRenamerDiff.ViewModels;

public class AddSerialNumberViewModel
{
    private readonly MainModel mainModel;

    public ReactivePropertySlim<int> StartNumber { get; } = new(AddSerialNumberRegex.DefaultStartNumber);
    public ReactivePropertySlim<int> Step { get; } = new(AddSerialNumberRegex.DefaultStep);
    public ReactivePropertySlim<int> ZeroPadCount { get; } = new(1);
    public ReactivePropertySlim<bool> IsDirectoryReset { get; } = new();
    public ReactivePropertySlim<bool> IsInverseOrder { get; } = new();

    /// <summary>
    /// 現在の設定のパターンへの追加
    /// </summary>
    public ReactiveCommand AddSettingCommand { get; }

    public ReadOnlyReactivePropertySlim<string> TextAddPattern { get; }
    public ReactivePropertySlim<string> TextTargetPattern { get; } = new("^");


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

        this.AddSettingCommand = Observable
            .CombineLatest(TextTargetPattern, TextAddPattern,
                (x, y) => x.HasText() && y.HasText())
            .ToReactiveCommand()
            .WithSubscribe(() =>
                mainModel.Setting.ReplaceTexts
                    .Add(new ReplacePattern(TextTargetPattern.Value, TextAddPattern.Value, true)));
    }

    private static Regex regexTailDefaultParamerters = new(",+(?=>)", RegexOptions.Compiled);
    private static Regex regexAllDefaultParamerters = new("<,*>", RegexOptions.Compiled);

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
}
