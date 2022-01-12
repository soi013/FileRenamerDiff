using System.IO.Abstractions;

namespace FileRenamerDiff.Models;

/// <summary>
/// 正規表現を用いて文字列を更新日時で置換する処理とパターンを保持するクラス
/// </summary>
public class AddUpdateTimeRegex : ReplaceRegexBase
{
    /// <summary>
    /// 「$$u」を含まない「$u」、「$u<"paramerter">」
    /// </summary>
    private const string targetWord = @"(?<!\$)\$u(<.+>)?";
    /// <summary>
    /// 「$u」が置換後文字列にあるか判定するRegex
    /// </summary>
    private static readonly Regex regexTargetWord = new(targetWord, RegexOptions.Compiled);

    /// <summary>
    /// 「$u<"paramerter">」の中の「paramerter」
    /// </summary>
    private const string paramerterWord = @"(?<=\<).+(?=\>)";
    /// <summary>
    /// paramerterを取得するRegex
    /// </summary>
    private static readonly Regex regexParamerterWord = new(paramerterWord, RegexOptions.Compiled);

    /// <summary>
    /// 「$u」を含んだ置換後文字列
    /// </summary>
    private readonly string replaceText;

    /// <summary>
    /// 連番文字列化時のフォーマット
    /// </summary>
    private readonly string? format;

    public AddUpdateTimeRegex(Regex regex, string replaceText) : base(regex)
    {
        this.replaceText = replaceText;

        string[] paramerters = regexParamerterWord.Match(replaceText).Value
            .Split(',');

        const string defaultFormat = "yyyy-MM-dd";

        this.format = paramerters.ElementAtOrDefault(0);
        format = format.HasText()
            ? format
            : defaultFormat;
    }

    internal override string Replace(string input, IReadOnlyList<string>? allPaths = null, IFileSystemInfo? fsInfo = null)
    {
        //「置換後文字列内の「$u」」を更新日時で置換する
        string lastWriteTimeText = fsInfo?.LastWriteTime.ToString(format) ?? string.Empty;

        var replaceTextModified = regexTargetWord.Replace(replaceText, lastWriteTimeText);

        //再帰的に置換パターンを作成して、RegexBaseを生成する
        var rpRegexModified = new ReplacePattern(regex.ToString(), replaceTextModified, true)
            .ToReplaceRegex();

        return rpRegexModified?.Replace(input, allPaths, fsInfo) ?? input;
    }

    /// <summary>
    /// 対象となるパターンを含むか判定
    /// </summary>
    /// <param name="replaceText">置換後文字列を指定</param>
    internal static bool IsContainPattern(string replaceText) => regexTargetWord.IsMatch(replaceText);
}
