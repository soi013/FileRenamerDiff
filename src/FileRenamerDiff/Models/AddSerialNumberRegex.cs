
using System.IO.Abstractions;

namespace FileRenamerDiff.Models;

/// <summary>
/// 正規表現を用いて文字列をディレクトリ名で置換する処理とパターンを保持するクラス
/// </summary>
public class AddSerialNumberRegex : ReplaceRegexBase
{
    //「$$n」を含まない「$n」、「$n<"paramerter">」
    private const string targetRegexWord = @"(?<!\$)\$n(<.+>)?";
    private const string paramerterRegexWord = @"(?<=\<).+(?=\>)";
    //「$n」が置換後文字列にあるか判定するRegex
    private static readonly Regex regexTargetWord = new(targetRegexWord, RegexOptions.Compiled);
    //パラメータを取得するRegex
    private static readonly Regex regexparamerterWord = new(paramerterRegexWord, RegexOptions.Compiled);

    /// <summary>
    /// 「$n」を含んだ置換後文字列
    /// </summary>
    private readonly string replaceText;
    private readonly int startNumber;
    private readonly int step;

    public AddSerialNumberRegex(Regex regex, string replaceText) : base(regex)
    {
        this.replaceText = replaceText;

        string[] paramerters = regexparamerterWord.Match(replaceText).Value
            .Split(',');

        this.startNumber = paramerters.ElementAtOrDefault(0)?.ToIntOrNull() ?? 1;
        this.step = paramerters.ElementAtOrDefault(1)?.ToIntOrNull() ?? 1;
    }

    internal override string Replace(string input, IReadOnlyList<string>? allPaths = null, IFileSystemInfo? fsInfo = null)
    {
        string inputPath = fsInfo?.FullName ?? string.Empty;
        allPaths ??= new[] { inputPath };

        //全てのパスの中でのこの番号を決定
        int indexPath = allPaths.WithIndex().FirstOrDefault(a => a.element == inputPath).index;

        //「置換後文字列内の「$n」」を連番で置換する
        string numberStr = (indexPath * step + startNumber).ToString();
        var replaceTextModified = regexTargetWord.Replace(replaceText, numberStr);

        //再帰的に置換パターンを作成して、RegexBaseを生成する
        var rpRegexModified = new ReplacePattern(regex.ToString(), replaceTextModified, true)
            .ToReplaceRegex();

        return rpRegexModified?.Replace(input, allPaths, fsInfo) ?? input;
    }

    /// <summary>
    /// AddDirectoryNameを含むか判定
    /// </summary>
    /// <param name="replaceText">置換後文字列を指定</param>
    internal static bool IsContainPattern(string replaceText) => regexTargetWord.IsMatch(replaceText);
}
