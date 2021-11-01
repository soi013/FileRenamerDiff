using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// 正規表現を用いて文字列を置換する処理とパターンを保持するクラス
    /// </summary>
    public class ReplaceRegex
    {
        readonly Regex regex;
        readonly string replaceText;
        readonly MatchEvaluator? matchEvaluator;
        readonly bool asAddFolder;

        /// <summary>
        /// 置換パターンを組み立てる
        /// </summary>
        public ReplaceRegex(Regex regex, string replaceText, bool asAddFolder = false)
        {
            this.regex = regex;
            this.replaceText = replaceText;
            this.matchEvaluator = SpecialReplacePattern.FindEvaluator(replaceText);
            this.asAddFolder = asAddFolder;
        }

        /// <summary>
        /// 置換実行
        /// </summary>
        internal string Replace(string input, IFileSystemInfo? fsInfo = null) =>
            regex == null ? input
            : matchEvaluator != null ? regex.Replace(input, matchEvaluator)
            : asAddFolder ? regex.Replace(input, fsInfo?.GetDirectoryName() ?? string.Empty)
            : regex.Replace(input, replaceText);

        public override string ToString() => $"{regex}->{replaceText}";
    }
}
