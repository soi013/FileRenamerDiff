using System.Text.RegularExpressions;

using Livet;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// 置換前後のパターン
    /// </summary>
    public class ReplacePattern : ReplacePatternBase
    {
        private string _ReplaceText;
        /// <summary>
        /// 置換後文字列
        /// </summary>
        public string ReplaceText
        {
            get => _ReplaceText;
            set => RaisePropertyChangedIfSet(ref _ReplaceText, value);
        }

        /// <summary>
        /// 置換パターンを組み立てる
        /// </summary>
        /// <param name="targetPattern">置換される対象のパターン</param>
        /// <param name="replaceText">置換後文字列</param>
        /// <param name="asExpression">パターンを単純一致か正規表現とするか</param>
        public ReplacePattern(string targetPattern, string replaceText = "", bool asExpression = false)
            : base(targetPattern, asExpression)
        {
            this._ReplaceText = replaceText;
        }

        public override ReplaceRegexBase? ToReplaceRegex()
        {
            var patternEx = AsExpression
                ? TargetPattern
                : Regex.Escape(TargetPattern);

            Regex? regex = AppExtension.CreateRegexOrNull(patternEx);

            return regex == null
                ? null
                : new ReplaceRegex(regex, ReplaceText);
        }

        public override string ToString() => $"{TargetPattern}->{ReplaceText}";
    }
}
