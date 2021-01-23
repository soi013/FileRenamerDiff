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

        /// <summary>
        /// 置換パターンを組み立てる
        /// </summary>
        public ReplaceRegex(Regex regex, string replaceText)
        {
            this.regex = regex;
            this.replaceText = replaceText;
            this.matchEvaluator = ConvertToMatchEvaluator(replaceText);
        }

        /// <summary>
        /// 置換実行
        /// </summary>
        internal string Replace(string input) =>
            regex == null ? input
            : matchEvaluator == null ? regex.Replace(input, replaceText)
            : regex.Replace(input, matchEvaluator);

        /// <summary>
        /// 特殊置換へ変換
        /// </summary>
        /// <param name="replaceText">置換文字列</param>
        /// <returns>特殊置換、なければnull</returns>
        MatchEvaluator? ConvertToMatchEvaluator(string replaceText)
        {
            var m = Regex.Match(replaceText, @"^\\([ul])\$(\d+)");
            if (!m.Success)
                return null;

            var group = int.Parse(m.Groups[2].Value);
            return m.Groups[1].Value == "u"
                ? (match => match.Groups[group].Value.ToUpper())
                : (match => match.Groups[group].Value.ToLower());
        }

        public override string ToString() => $"{regex}->{replaceText}";
    }
}