using Livet;

namespace FileRenamerDiff.Models
{
    public class ReplacePattern : NotificationObject
    {
        private string _Pattern;
        public string Pattern
        {
            get => _Pattern;
            set => RaisePropertyChangedIfSet(ref _Pattern, value);
        }

        private string _ReplaceText;
        public string ReplaceText
        {
            get => _ReplaceText;
            set => RaisePropertyChangedIfSet(ref _ReplaceText, value);
        }

        private bool _AsExpression;
        public bool AsExpression
        {
            get => _AsExpression;
            set => RaisePropertyChangedIfSet(ref _AsExpression, value);
        }

        public ReplacePattern(string pattern, string replaceText, bool asExpression = false)
        {
            this.Pattern = pattern;
            this.AsExpression = asExpression;
            this.ReplaceText = replaceText;
        }
        public override string ToString() => $"{Pattern}->{ReplaceText}";
    }
}