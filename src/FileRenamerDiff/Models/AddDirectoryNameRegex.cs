using System;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace FileRenamerDiff.Models;

/// <summary>
/// 正規表現を用いて文字列をディレクトリ名で置換する処理とパターンを保持するクラス
/// </summary>
public class AddDirectoryNameRegex : ReplaceRegexBase
{
    private static readonly string targetRegexWord = @"(?<!\$)\$d";
    private static readonly Regex regexTargetWord = new(targetRegexWord, RegexOptions.Compiled);

    private readonly string replaceText;

    public AddDirectoryNameRegex(Regex regex, string replaceText) : base(regex)
    {
        this.replaceText = replaceText;
    }

    internal override string Replace(string input, IFileSystemInfo? fsInfo = null)
    {
        //「置換後文字列($d)」をディレクトリ名で置換する
        string directoryName = fsInfo?.GetDirectoryName() ?? string.Empty;
        var replaceTextModified = regexTargetWord.Replace(replaceText, directoryName);

        //再帰的に置換パターンを作成して、RegexBaseを生成する
        var rpRegexModified = new ReplacePattern(regex.ToString(), replaceTextModified, true)
            .ToReplaceRegex();

        return rpRegexModified?.Replace(input, fsInfo) ?? input;
    }

    /// <summary>
    /// AddDirectoryNameを含むか判定
    /// </summary>
    /// <param name="replaceText">置換後文字列を指定</param>
    internal static bool IsContainPattern(string replaceText) => regexTargetWord.IsMatch(replaceText);
}
