using System.Text.RegularExpressions;

using Livet;

namespace FileRenamerDiff.Models
{
    public abstract class ReplacePatternBase : NotificationObject
    {
        private string _TargetPattern;
        /// <summary>
        /// 置換される対象のパターン
        /// </summary>
        public string TargetPattern
        {
            get => _TargetPattern;
            set => RaisePropertyChangedIfSet(ref _TargetPattern, value);
        }

        private bool _AsExpression;
        /// <summary>
        /// パターンを単純一致か正規表現とするか
        /// </summary>
        public bool AsExpression
        {
            get => _AsExpression;
            set => RaisePropertyChangedIfSet(ref _AsExpression, value);
        }

        /// <summary>
        /// 置換パターンを組み立てる
        /// </summary>
        /// <param name="targetPattern">置換される対象のパターン</param>
        /// <param name="asExpression">パターンを単純一致か正規表現とするか</param>
        public ReplacePatternBase(string targetPattern, bool asExpression = false)
        {
            this._TargetPattern = targetPattern;
            this._AsExpression = asExpression;
        }

        public abstract ReplaceRegexBase? ToReplaceRegex();

        internal static ReplacePattern CreateEmpty() => new(string.Empty, string.Empty);

        internal static AddDirectoryNamePattern CreateAddFolder(string targetFileName, bool asExpression = false)
            => new(targetFileName, asExpression);
    }
}
