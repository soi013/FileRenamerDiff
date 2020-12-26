﻿using FileRenamerDiff.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// よく使う設定パターン集
    /// </summary>
    /// <param name="Comment">パターン説明</param>
    /// <param name="TargetPattern">置換される対象のパターン</param>
    /// <param name="ReplaceText">置換後文字列</param>
    /// <param name="SampleInput">サンプル入力例</param>
    /// <param name="AsExpression">パターンを単純一致か正規表現とするか</param>
    public record CommonPattern(string Comment, string TargetPattern, string ReplaceText, string SampleInput, bool AsExpression)
    {
        /// <summary>
        /// 置換パターンへの変換
        /// </summary>
        public ReplacePattern ToReplacePattern() => new(TargetPattern, ReplaceText, AsExpression);

        /// <summary>
        /// サンプル出力例
        /// </summary>
        public string SampleOutput { get; } = new ReplacePattern(TargetPattern, ReplaceText, AsExpression)
            .ToReplaceRegex()?.Replace(SampleInput)
            ?? String.Empty;

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
                (Resources.Common_DeleteIgnoreCase,            "(?i)abc",                             "ABC_abc_AbC.txt",  true),
            }
            .Select(a => new CommonPattern(a.comment, a.target, "", a.sample, a.exp))
            .ToArray();

        /// <summary>
        /// よく使う置換パターン集
        /// </summary>
        public static IReadOnlyList<CommonPattern> ReplacePatterns { get; } =
            new CommonPattern[]
            {
                new(Resources.Common_SurroundWithSqureBrackets, "ABC", "[$0]", "xABCx_AxBC.txt", true),
                new(Resources.Common_ReduceWhiteSpace,  "\\s+", " ", "A B　C.txt", true),
                new(Resources.Common_ReplaceWhiteSpacesWithUnderbar,  "\\s+", "_", "A B　C.txt", true),

                new(Resources.Common_AddThreeZero,  "\\d+",  "00$0", "Sapmle-12.txt", true),
                new(Resources.Common_TakeNumberThreeDigits, "\\d*(\\d{3})", "$1", "Sapmle-0012.txt", true),

                new(Resources.Common_ReplaceAllExtentionToABC,  "\\.\\w+$", ".ABC", "sam.ple.txt", true),
                new(Resources.Common_ReplaceIgnoreCase,  "(?i)abc", "x", "ABC_abc_AbC.txt", true),
            };
    }
}