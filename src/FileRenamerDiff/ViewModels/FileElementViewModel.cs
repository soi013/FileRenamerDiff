﻿using System.Diagnostics;
using System.Reactive.Linq;

using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;

using Livet;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace FileRenamerDiff.ViewModels;

/// <summary>
/// リネーム前後のファイル名を含むファイル情報ViewModel
/// </summary>
public class FileElementViewModel : ViewModel
{
    public FileElementModel PathModel { get; }

    /// <summary>
    /// リネーム前後の差分比較情報
    /// </summary>
    public ReadOnlyReactivePropertySlim<SideBySideDiffModel?> Diff { get; }

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
    public string DirectoryPath => PathModel.DirectoryPath;

    /// <summary>
    /// ファイルのバイト数
    /// </summary>
    public long LengthByte => PathModel.LengthByte;

    /// <summary>
    /// ファイル更新日時の現在のカルチャでの文字列
    /// </summary>
    public string LastWriteTime => PathModel.LastWriteTime.ToString();

    /// <summary>
    /// ファイル作成日時の現在のカルチャでの文字列
    /// </summary>
    public string CreationTime => PathModel.CreationTime.ToString();

    /// <summary>
    /// ファイル種類
    /// </summary>
    public FileCategories Category => PathModel.Category;

    /// <summary>
    /// エクスプローラーで開くコマンド
    /// </summary>
    public ReactiveCommand OpenInExploreCommand { get; } = new();

    /// <summary>
    /// ModelをもとにViewModelを作成
    /// </summary>
    public FileElementViewModel(FileElementModel pathModel)
    {
        this.PathModel = pathModel;

        this.Diff = Observable.CombineLatest(
                pathModel.ObserveProperty(x => x.InputFileName),
                pathModel.ObserveProperty(x => x.OutputFileName),
                (i, o) => AppExtension.CreateDiff(i, o))
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

    public override string ToString() => PathModel.ToString();
}
