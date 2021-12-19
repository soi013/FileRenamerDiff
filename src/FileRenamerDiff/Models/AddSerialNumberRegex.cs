using System.IO.Abstractions;

namespace FileRenamerDiff.Models;

/// <summary>
/// 正規表現を用いて文字列を連番で置換する処理とパターンを保持するクラス
/// </summary>
public class AddSerialNumberRegex : ReplaceRegexBase
{
    /// <summary>
    /// 「$$n」を含まない「$n」、「$n<"paramerter">」
    /// </summary>
    private const string targetWord = @"(?<!\$)\$n(<.+>)?";
    /// <summary>
    /// 「$n」が置換後文字列にあるか判定するRegex
    /// </summary>
    private static readonly Regex regexTargetWord = new(targetWord, RegexOptions.Compiled);

    /// <summary>
    /// 「$n<"paramerter">」の中の「paramerter」
    /// </summary>
    private const string paramerterWord = @"(?<=\<).+(?=\>)";
    /// <summary>
    /// paramerterを取得するRegex
    /// </summary>
    private static readonly Regex regexParamerterWord = new(paramerterWord, RegexOptions.Compiled);

    /// <summary>
    /// 「$n」を含んだ置換後文字列
    /// </summary>
    private readonly string replaceText;

    /// <summary>
    /// 連番開始番号（デフォルト=1）
    /// </summary>
    private readonly int startNumber;

    /// <summary>
    /// 連番ステップ数（デフォルト=1）
    /// </summary>
    private readonly int step;

    /// <summary>
    /// 連番文字列化時のフォーマット
    /// </summary>
    private readonly string? format;

    /// <summary>
    /// ディレクトリごと連番有効状態
    /// </summary>
    private readonly bool isDirectoryReset;

    /// <summary>
    /// 逆順有効状態
    /// </summary>
    private readonly bool isInverseOrder;

    public AddSerialNumberRegex(Regex regex, string replaceText) : base(regex)
    {
        this.replaceText = replaceText;

        string[] paramerters = regexParamerterWord.Match(replaceText).Value
            .Split(',');

        this.startNumber = paramerters.ElementAtOrDefault(0)?.ToIntOrNull() ?? 1;
        this.step = paramerters.ElementAtOrDefault(1)?.ToIntOrNull() ?? 1;
        this.format = paramerters.ElementAtOrDefault(2);
        this.isDirectoryReset = paramerters.ElementAtOrDefault(3) == "r";
        this.isInverseOrder = paramerters.ElementAtOrDefault(4) == "i";
    }

    internal override string Replace(string input, IReadOnlyList<string>? allPaths = null, IFileSystemInfo? fsInfo = null)
    {
        string inputPath = fsInfo?.FullName ?? string.Empty;
        allPaths ??= new[] { inputPath };

        //ディレクトリごと連番が有効なら、ディレクトリパスが同じものだけに絞る
        if (isDirectoryReset)
        {
            allPaths = allPaths
                .Where(x => Path.GetDirectoryName(x) == Path.GetDirectoryName(inputPath))
                .ToArray();
        }
        //逆順なら反転する
        if (isInverseOrder)
        {
            allPaths = allPaths.Reverse().ToArray();
        }

        //全てのパスの中でのこの番号を決定
        int indexPath = allPaths.WithIndex().FirstOrDefault(a => a.element == inputPath).index;

        //「置換後文字列内の「$n...」」を連番で置換する
        string numberStr = (indexPath * step + startNumber).ToString(format);
        var replaceTextModified = regexTargetWord.Replace(replaceText, numberStr);

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
