using System.Text.RegularExpressions;

using Livet;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// フォルダ名追加パターン
    /// </summary>
    public class AddDirectoryNamePattern : ReplacePatternBase
    {
        /// <summary>
        /// フォルダ名追加パターンを組み立てる
        /// </summary>
        /// <param name="targetPattern">追加・置換される対象のパターン</param>
        /// <param name="asExpression">パターンを単純一致か正規表現とするか</param>
        public AddDirectoryNamePattern(string targetPattern, bool asExpression = false)
            : base(targetPattern, asExpression)
        { }

        public override ReplaceRegexBase? ToReplaceRegex()
        {
            var patternEx = AsExpression
                ? TargetPattern
                : Regex.Escape(TargetPattern);

            Regex? regex = AppExtension.CreateRegexOrNull(patternEx);

            return regex == null
                ? null
                : new AddDirectoryNameRegex(regex);
        }

        public override string ToString() => $"{TargetPattern}-><FolderName>";
    }
}
