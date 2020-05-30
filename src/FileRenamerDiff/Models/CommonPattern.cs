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
            ("Delete Windows copy tag", Resources.Windows_CopyFileSuffix, false),
            ("Delete Windows shortcut tag", Resources.Windows_ShortcutFileSuffix, false),
            ("Delete (number) tag", "\\([0-9]{0,3}\\)", true),
        }
        .Select(a => new CommonPattern(a.comment, new ReplacePattern(a.target, "", a.exp)))
        .ToArray();

        /// <summary>
        /// よく使う置換パターン集
        /// </summary>
        public static IReadOnlyList<CommonPattern> ReplacePatterns { get; } =
            new (string comment, string target, string replace, bool exp)[]
        {
            ("Surround ABC with [].", "ABC","[$0]" , true),
            ("Reduce whitespaces to one single-byte space","\\s+", " ", true),
            ("Replace whitespaces with '_'","\\s+", "_",true),
            ("Add three [0] to the number (three-digit zero padding 1/2)", "\\d+","00$0",true),
            ("Take the number to three digits (three digit zero padding 2/2)","\\d*(\\d{3})", "$1",true),
        }
        .Select(a => new CommonPattern(a.comment, new ReplacePattern(a.target, a.replace, a.exp)))
        .ToArray();
    }
}