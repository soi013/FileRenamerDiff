using System.Text.RegularExpressions;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// 正規表現を用いて文字列を置換する処理とパターンを保持するクラス
    /// </summary>
    public class ReplaceRegex
    {
        private Regex regex;
        private string replaceText;

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
            regex?.Replace(input, replaceText)
            ?? input;

        public override string ToString() => $"{regex}->{replaceText}";
    }
}