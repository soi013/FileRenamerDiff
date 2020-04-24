using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using Livet;

namespace FileRenamerDiff.Models
{
    public class FilePathModel : NotificationObject
    {
        private Model model => Model.Instance;
        private SettingAppModel settingApp => model.Setting;

        private readonly string path;
        private readonly FileInfo fileInfo;

        public string FileName => fileInfo?.Name;

        private string outputFileName = "--.-";
        public string OutputFileName
        {
            get => outputFileName;
            set => RaisePropertyChangedIfSet(ref outputFileName, value, nameof(IsReplaced));
        }

        private string replacedPath => Path.Combine(DirectoryPath, outputFileName);

        public bool IsReplaced => FileName != OutputFileName;


        public string DirectoryPath => fileInfo.DirectoryName;
        public long LengthByte => fileInfo.Exists ? fileInfo.Length : -1;
        public DateTime LastWriteTime => fileInfo.LastWriteTime;
        public DateTime CreationTime => fileInfo.CreationTime;
        public FileAttributes Attributes => fileInfo.Attributes;

        public FilePathModel(string path)
        {
            this.path = path;

            this.fileInfo = new FileInfo(path);

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

        internal void Rename()
        {
            Trace.WriteLine($"info Rename [{FileName}] -> [{OutputFileName}] in [{DirectoryPath}]");
            if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                Directory.Move(this.path, this.replacedPath);
            else
                fileInfo.MoveTo(replacedPath);
        }
    }
}