
using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;

using Reactive.Bindings;

namespace FileRenamerDiff.ViewModels;

/// <summary>
/// よく使うパターン集ViewModel
/// </summary>
public class CommonPatternViewModel
{
    private readonly CommonPattern modelPattern;

    /// <summary>
    /// パターン説明
    /// </summary>
    public string Comment => modelPattern.Comment;
    /// <summary>
    /// 置換されるパターン
    /// </summary>
    public string TargetPattern => modelPattern.TargetPattern;
    /// <summary>
    /// 置換後文字列（削除パターンの場合は非表示）
    /// </summary>
    public string ReplaceText => modelPattern.ReplaceText;

    /// <summary>
    /// パターンを単純一致か正規表現とするか
    /// </summary>
    public bool AsExpression => modelPattern.AsExpression;

    /// <summary>
    /// サンプル入出力の比較情報
    /// </summary>
    public SideBySideDiffModel SampleDiff { get; }

    /// <summary>
    /// 現在の設定のパターンへの追加
    /// </summary>
    public ReactiveCommand AddSettingCommand { get; } = new();

    public CommonPatternViewModel(MainModel mainModel, CommonPattern modelPattern, bool isDelete)
    {
        this.modelPattern = modelPattern;
        this.SampleDiff = AppExtension.CreateDiff(modelPattern.SampleInput, modelPattern.SampleOutput);

        AddSettingCommand.Subscribe(() =>
            (isDelete ? mainModel.Setting.DeleteTexts : mainModel.Setting.ReplaceTexts)
            .Add(modelPattern.ToReplacePattern()));
    }
}
