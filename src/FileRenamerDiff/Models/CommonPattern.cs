using FileRenamerDiff.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
        public static IReadOnlyList<CommonPattern> DeletePatterns { get; } =
            new (string comment, string target, bool exp)[]
        {
            ("Delete Windows copy tag", Resources.Windows_CopyFilePostFix,false),
            ("Delete (number) tag", "\\([0-9]{0,3}\\)",true),
        }
        .Select(a => new CommonPattern(a.comment, new ReplacePattern(a.target, "", a.exp)))
        .ToArray();
    }
}