using System.Text.RegularExpressions;

namespace FileRenamerDiff.Models
{
    public class ReplaceRegex
    {
        private Regex regex;
        private string replaceText;

        public ReplaceRegex(string pattern, bool asExpression, string replaceText)
        {
            var patternEx = asExpression ? pattern : Regex.Escape(pattern);
            this.regex = new Regex(patternEx, RegexOptions.Compiled);
            this.replaceText = replaceText;
        }

        public ReplaceRegex(ReplacePattern a)
            : this(a.Pattern, a.AsExpression, a.ReplaceText) { }

        internal string Replace(string input) => regex.Replace(input, replaceText);

        public override string ToString() => $"{regex}->{replaceText}";
    }
}