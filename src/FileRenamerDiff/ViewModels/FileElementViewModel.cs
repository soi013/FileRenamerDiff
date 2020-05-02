
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
    /// <summary>
    /// リネーム前後のファイル名を含むファイル情報ViewModel
    /// </summary>
    public class FileElementViewModel : ViewModel
    {
        Model model = Model.Instance;
        private readonly FileElementModel pathModel;

        /// <summary>
        /// リネーム前後の差分比較情報
        /// </summary>
        public ReadOnlyReactivePropertySlim<SideBySideDiffModel> Diff { get; }

        /// <summary>
        /// リネーム前後で差があったか
        /// </summary>
        public ReadOnlyReactivePropertySlim<bool> IsReplaced { get; }

        /// <summary>
        /// 他のファイルパスと衝突しているか
        /// </summary>
        public ReadOnlyReactivePropertySlim<bool> IsConflicted { get; }

        /// <summary>
        /// ファイルの所属しているディレクトリ名
        /// </summary>
        public string DirectoryPath => pathModel.DirectoryPath;

        /// <summary>
        /// ファイルのバイト数を読みやすく変換したもの
        /// </summary>
        public string LengthByte => AppExtention.ReadableByteText(pathModel.LengthByte);

        /// <summary>
        /// ファイル更新日時の現在のカルチャでの文字列
        /// </summary>
        public string LastWriteTime => pathModel.LastWriteTime.ToString();

        /// <summary>
        /// ファイル作成日時の現在のカルチャでの文字列
        /// </summary>
        public string CreationTime => pathModel.CreationTime.ToString();

        /// <summary>
        /// ModelをもとにViewModelを作成
        /// </summary>
        public FileElementViewModel(FileElementModel pathModel)
        {
            this.pathModel = pathModel;

            this.Diff = pathModel
                .ObserveProperty(x => x.OutputFileName)
                .Select(x => CreateDiff())
                .ToReadOnlyReactivePropertySlim();

            this.IsReplaced = pathModel
                .ObserveProperty(x => x.IsReplaced)
                .ToReadOnlyReactivePropertySlim();

            this.IsConflicted = pathModel
                .ObserveProperty(x => x.IsConflicted)
                .ToReadOnlyReactivePropertySlim();
        }

        /// <summary>
        /// 差分比較情報を作成
        /// </summary>
        private SideBySideDiffModel CreateDiff()
        {
            var diff = new SideBySideDiffBuilder(new Differ());
            return diff.BuildDiffModel(pathModel.InputFileName, pathModel.OutputFileName);
        }

        public override string ToString() => $"Source:{Diff.Value?.ToDisplayString()}";
    }
}