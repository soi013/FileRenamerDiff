using System.IO.Abstractions;

namespace FileRenamerDiff.Models;

/// <summary>
/// 正規表現を用いて文字列を置換する処理とパターンを保持するクラス
/// </summary>
public class ReplaceRegex : ReplaceRegexBase
{
    readonly string replaceText;
    readonly MatchEvaluator? matchEvaluator;

    /// <summary>
    /// 置換パターンを組み立てる
    /// </summary>
    public ReplaceRegex(Regex regex, string replaceText)
        : base(regex)
    {
        this.replaceText = replaceText;
        this.matchEvaluator = SpecialReplacePattern.FindEvaluator(replaceText);
    }

    /// <summary>
    /// 置換実行
    /// </summary>
    internal override string Replace(string input, IReadOnlyList<string>? allPaths = null, IFileSystemInfo? fsInfo = null) =>
        regex == null ? input
        : matchEvaluator != null ? regex.Replace(input, matchEvaluator)
        : regex.Replace(input, replaceText);

    public override string ToString() => $"{regex}->{replaceText}";
}
