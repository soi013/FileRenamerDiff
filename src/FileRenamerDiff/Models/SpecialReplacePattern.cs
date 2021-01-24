using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Zipangu;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// 特殊置換パターン
    /// </summary>
    public class SpecialReplacePattern
    {
        /// <summary>
        /// 特殊置換にあてはまるかの判定用Regex
        /// </summary>
        public Regex MatchRegex { get; }
        /// <summary>
        /// 特殊置換のMatchEvaluatorを生成するデリゲート
        /// </summary>
        public Func<int, MatchEvaluator> EvaluatorCreator { get; }

        private SpecialReplacePattern(string matchPattern, Func<string, string> replaceFunc)
        {
            this.MatchRegex = new Regex(matchPattern, RegexOptions.Compiled);
            this.EvaluatorCreator = (groupIndex => (match => replaceFunc(match.Groups[groupIndex].Value)));
        }

        /// <summary>
        /// 指定された置換文字列が特殊置換にあてはまるなら、特殊置換のMatchEvaluatorを生成する
        /// </summary>
        /// <param name="replaceText">置換文字列</param>
        /// <returns>特殊置換、なければnull</returns>
        private MatchEvaluator? ConvertToEvaluator(string replaceText)
        {
            Match? matchResult = this.MatchRegex.Match(replaceText);

            if (matchResult?.Success != true || matchResult.Groups.Count <= 2)
                return null;

            int groupIndex = int.Parse(matchResult.Groups[2].Value);
            return this.EvaluatorCreator(groupIndex);
        }

        /// <summary>
        /// 特殊置換パターンリスト
        /// </summary>
        public static IReadOnlyList<SpecialReplacePattern> Patterns { get; } =
            new SpecialReplacePattern[]
            {
                new SpecialReplacePattern(@"^\\(u)\$(\d+)",x=>x.ToUpper()),
                new SpecialReplacePattern(@"^\\(l)\$(\d+)",x=>x.ToLower()),
                new SpecialReplacePattern(@"^\\(h)\$(\d+)",x=>x.AsciiToNarrow()),
                new SpecialReplacePattern(@"^\\(f)\$(\d+)",x=>x.AsciiToWide()),
            };


        /// <summary>
        /// 特殊置換MatchEvaluatorへ変換
        /// </summary>
        /// <param name="replaceText">置換文字列</param>
        /// <returns>特殊置換、なければnull</returns>
        internal static MatchEvaluator? FindEvaluator(string replaceText) =>
            Patterns
                .Select(p => p.ConvertToEvaluator(replaceText))
                .WhereNotNull()
                .FirstOrDefault();
    }
}