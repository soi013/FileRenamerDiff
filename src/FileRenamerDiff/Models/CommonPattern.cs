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
        /// サンプル入力例
        /// </summary>
        public string SampleInput { get; }
        /// <summary>
        /// サンプル出力例
        /// </summary>
        public string SampleOutput { get; }

        /// <summary>
        /// パターンを単純一致か正規表現とするか
        /// </summary>
        public bool AsExpression { get; }

        public CommonPattern(string comment, string targetPattern, string replaceText, string sampleInput, bool asExpression)
        {
            this.Comment = comment;
            this.TargetPattern = targetPattern;
            this.ReplaceText = replaceText;
            this.SampleInput = sampleInput;
            this.AsExpression = asExpression;

            var pattern = ToReplacePattern();
            this.SampleOutput = new ReplaceRegex(pattern).Replace(SampleInput);
        }

        /// <summary>
        /// 置換パターンへの変換
        /// </summary>
        public ReplacePattern ToReplacePattern() => new ReplacePattern(TargetPattern, ReplaceText, AsExpression);


        /// <summary>
        /// よく使う削除パターン集
        /// </summary>
        public static IReadOnlyList<CommonPattern> DeletePatterns { get; } =
            new (string comment, string target, string sample, bool exp)[]
        {
            (Resources.Common_DeleteWindowsCopyTag,     Resources.Windows_CopyFileSuffix,       $"Sample{Resources.Windows_CopyFileSuffix}.txt", false),
            (Resources.Common_DeleteWindowsShortcutTag, Resources.Windows_ShortcutFileSuffix,   $"Sample.txt{Resources.Windows_ShortcutFileSuffix}", false),
            (Resources.Common_DeleteWindowsNumberTag,         "\\s*\\([0-9]{0,3}\\)",                 "Sample (1).txt", true),
            (Resources.Common_DeleteWhitespacesHeadTail, "^\\s+|\\s+$",                  " Sample .txt ", true),
            (Resources.Common_DeleteExtention,            "\\.\\w+$",                             "sam.ple.txt",  true),
        }
        .Select(a => new CommonPattern(a.comment, a.target, "", a.sample, a.exp))
        .ToArray();

        /// <summary>
        /// よく使う置換パターン集
        /// </summary>
        public static IReadOnlyList<CommonPattern> ReplacePatterns { get; } =
            new (string comment, string target, string replace, string sample, bool exp)[]
        {
            (Resources.Common_SurroundWithSqureBrackets,    "ABC",      "[$0]",     "xABCx_AxBC.txt",   true),
            (Resources.Common_ReduceWhiteSpace,             "\\s+",     " ",        "A  B　C.txt",   true),
            (Resources.Common_ReplaceWhiteSpacesWithUnderbar,                "\\s+",     "_",        "A  B　C.txt",   true),

            (Resources.Common_AddThreeZero,             "\\d+",         "00$0", "Sapmle-12.txt",    true),
            (Resources.Common_TakeNumberThreeDigits,    "\\d*(\\d{3})", "$1",   "Sapmle-0012.txt",  true),
            (Resources.Common_ReplaceAllExtentionToABC,                "\\.\\w+$", ".ABC",      "sam.ple.txt",  true),
        }
        .Select(a => new CommonPattern(a.comment, a.target, a.replace, a.sample, a.exp))
        .ToArray();
    }
}