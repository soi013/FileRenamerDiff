using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace FileRenamerDiff.Models
{
    internal class AddDirectoryNameRegex : ReplaceRegexBase
    {
        public AddDirectoryNameRegex(Regex regex) : base(regex) { }

        internal override string Replace(string input, IFileSystemInfo? fsInfo = null)
            => regex.Replace(input, fsInfo?.GetDirectoryName() ?? string.Empty);
    }
}
