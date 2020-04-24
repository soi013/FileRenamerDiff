
using System;
using System.IO;
using System.Linq;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

using DiffPlex.DiffBuilder;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;

namespace FileRenamerDiff.ViewModels
{
    public class FilePathViewModel : ViewModel
    {
        Model model = Model.Instance;
        private readonly FilePathModel pathModel;

        public ReadOnlyReactivePropertySlim<SideBySideDiffModel> Diff { get; }
        public ReadOnlyReactivePropertySlim<bool> IsReplaced { get; }

        public string DirectoryPath => pathModel.DirectoryPath;

        public string LengthByte => AppExtention.ReadableByteText(pathModel.LengthByte);

        public string LastWriteTime => pathModel.LastWriteTime.ToString();

        public string CreationTime => pathModel.CreationTime.ToString();

        public FilePathViewModel(FilePathModel pathModel)
        {
            this.pathModel = pathModel;

            this.Diff = pathModel
                .ObserveProperty(x => x.OutputFileName)
                .Select(x => CreateDiff())
                .ToReadOnlyReactivePropertySlim();

            this.IsReplaced = pathModel
                .ObserveProperty(x => x.IsReplaced)
                .ToReadOnlyReactivePropertySlim();
        }

        private SideBySideDiffModel CreateDiff()
        {
            var diff = new SideBySideDiffBuilder(new Differ());
            return diff.BuildDiffModel(pathModel.FileName, pathModel.OutputFileName);
        }

        public override string ToString() => $"Source:{Diff.Value?.ToDisplayString()}";
    }
}