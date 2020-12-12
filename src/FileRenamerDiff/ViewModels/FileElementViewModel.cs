using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Resources;
using System.Globalization;
using System.Windows.Data;
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using Reactive.Bindings;
using System.Reactive;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using Anotar.Serilog;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;
using FileRenamerDiff.Properties;

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
        /// ファイルのバイト数
        /// </summary>
        public long LengthByte => pathModel.LengthByte;

        /// <summary>
        /// ファイル更新日時の現在のカルチャでの文字列
        /// </summary>
        public string LastWriteTime => pathModel.LastWriteTime.ToString();

        /// <summary>
        /// ファイル作成日時の現在のカルチャでの文字列
        /// </summary>
        public string CreationTime => pathModel.CreationTime.ToString();

        /// <summary>
        /// エクスプローラーで開くコマンド
        /// </summary>
        public ReactiveCommand OpenInExploreCommand { get; } = new ReactiveCommand();

        /// <summary>
        /// ModelをもとにViewModelを作成
        /// </summary>
        public FileElementViewModel(FileElementModel pathModel)
        {
            this.pathModel = pathModel;

            this.Diff = pathModel
                .ObserveProperty(x => x.OutputFileName)
                .Select(x => AppExtention.CreateDiff(pathModel.InputFileName, pathModel.OutputFileName))
                .ToReadOnlyReactivePropertySlim();

            this.IsReplaced = pathModel
                .ObserveProperty(x => x.IsReplaced)
                .ToReadOnlyReactivePropertySlim();

            this.IsConflicted = pathModel
                .ObserveProperty(x => x.IsConflicted)
                .ToReadOnlyReactivePropertySlim();

            OpenInExploreCommand.Subscribe(x =>
                Process.Start("EXPLORER.EXE", @$"/select,""{pathModel.InputFilePath}"""));
        }

        public override string ToString() => $"Source:{Diff.Value?.ToDisplayString()}";
    }
}