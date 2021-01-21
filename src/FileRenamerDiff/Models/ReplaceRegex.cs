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

        /// <summary>
        /// 置換パターンを組み立てる
        /// </summary>
        public ReplaceRegex(Regex regex, string replaceText)
        {
            this.regex = regex;
            this.replaceText = replaceText;
        }

        /// <summary>
        /// 置換実行
        /// </summary>
        internal string Replace(string input) =>
            regex?.Replace(input, ConvertToMatchEvaluator(replaceText))
                ?? input;

        /// <summary>
        /// 特殊置換
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static MatchEvaluator ConvertToMatchEvaluator(string s)
        {
            var m = Regex.Match(s, @"^\\([uU])\$(\d+)");
            if (!m.Success)
                return _ => s;

            var group = int.Parse(m.Groups[2].Value);
            return m.Groups[1].Value == "U"
                ? (MatchEvaluator)(match => match.Groups[group].Value.ToUpper())
                : (match => match.Groups[group].Value.ToLower());
        }

        public override string ToString() => $"{regex}->{replaceText}";
    }
}