using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.IO;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;
using FileRenamerDiff.Models;
using System.Reactive.Linq;
using DiffPlex.DiffBuilder;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;
using System.Linq;

namespace FileRenamerDiff.ViewModels
{
    public class FilePathViewModel : ViewModel
    {
        Model model = Model.Instance;
        private readonly FilePathModel pathModel;

        public string DirectoryPath => pathModel.DirectoryPath;

        public ReadOnlyReactivePropertySlim<SideBySideDiffModel> Diff { get; }
        public ReadOnlyReactivePropertySlim<bool> IsReplaced { get; }

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