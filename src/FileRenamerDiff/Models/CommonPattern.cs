using FileRenamerDiff.Properties;
using System.Collections.Generic;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// よく使う設定パターン集
    /// </summary>
    public class CommonPattern
    {
        /// <summary>
        /// パターン説明
        /// </summary>
        public string Comment { get; }

        /// <summary>
        /// パターン
        /// </summary>
        public ReplacePattern ReplacePattern { get; }

        public CommonPattern(string comment, ReplacePattern replacePattern)
        {
            this.Comment = comment;
            this.ReplacePattern = replacePattern;
        }

        /// <summary>
        /// よく使う削除パターン集
        /// </summary>
        public static IReadOnlyList<CommonPattern> DeletePatterns { get; } = new[]
        {
            new CommonPattern("Delete Windows copy tag", new ReplacePattern(Resources.Windows_CopyFilePostFix,"",false)),
            new CommonPattern("Delete (number) tag", new ReplacePattern("\\([0-9]{0,3}\\)","",true)),
        };
    }
}