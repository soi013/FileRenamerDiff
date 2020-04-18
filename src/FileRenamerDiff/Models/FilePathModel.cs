using Livet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FileRenamerDiff.Models
{
    public class FilePathModel : NotificationObject
    {
        private Model model => Model.Instance;
        private SettingAppModel settingApp => model.Setting;

        private readonly string path;

        public string DirectoryPath => Path.GetDirectoryName(path);

        public string FileName => Path.GetFileName(path);

        private string outputFileName = "--.-";
        public string OutputFileName
        {
            get => outputFileName;
            set => RaisePropertyChangedIfSet(ref outputFileName, value, nameof(IsReplaced));
        }

        private string replacedPath => Path.Combine(DirectoryPath, outputFileName);

        public bool IsReplaced => FileName != OutputFileName;



        public FilePathModel(string path)
        {
            this.path = path;
            this.outputFileName = FileName;
        }

        internal void Replace(List<ReplaceRegex> repRegexes)
        {
            var outFileName = FileName;

            foreach (var reg in repRegexes)
            {
                outFileName = reg.Replace(outFileName);
            }

            OutputFileName = outFileName;
        }
        public override string ToString() => $"{FileName}->{OutputFileName}";

        internal void Rename() => File.Move(path, replacedPath);
    }
}