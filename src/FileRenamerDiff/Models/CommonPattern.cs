using FileRenamerDiff.Properties;
using System;
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
        /// 置換される対象のパターン
        /// </summary>
        public string TargetPattern { get; }

        /// <summary>
        /// 置換後文字列
        /// </summary>
        public string ReplaceText { get; }

        /// <summary>
        /// パターンを単純一致か正規表現とするか
        /// </summary>
        public bool AsExpression { get; }

        public CommonPattern(string comment, string targetPattern, string replaceText, bool asExpression)
        {
            this.Comment = comment;
            this.TargetPattern = targetPattern;
            this.ReplaceText = replaceText;
            this.AsExpression = asExpression;
        }

        /// <summary>
        /// 置換パターンへの変換
        /// </summary>
        public ReplacePattern ToReplacePattern() => new ReplacePattern(TargetPattern, ReplaceText, AsExpression);


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
        .Select(a => new CommonPattern(a.comment, a.target, "", a.exp))
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
        .Select(a => new CommonPattern(a.comment, a.target, a.replace, a.exp))
        .ToArray();
    }
}