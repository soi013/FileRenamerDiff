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
using System.IO;
using System.Threading.Tasks;

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
using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;
using FileRenamerDiff.Properties;

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// よく使うパターン集ViewModel
    /// </summary>
    public class CommonPatternViewModel
    {
        protected Model model = Model.Instance;

        readonly CommonPattern modelPattern;

        /// <summary>
        /// パターン説明
        /// </summary>
        public string Comment => modelPattern.Comment;
        /// <summary>
        /// 置換されるパターン
        /// </summary>
        public string TargetPattern => modelPattern.TargetPattern;
        /// <summary>
        /// 置換後文字列（削除パターンの場合は非表示）
        /// </summary>
        public string ReplaceText => modelPattern.ReplaceText;

        /// <summary>
        /// パターンを単純一致か正規表現とするか
        /// </summary>
        public bool AsExpression => modelPattern.AsExpression;

        /// <summary>
        /// サンプル入出力の比較情報
        /// </summary>
        public SideBySideDiffModel SampleDiff { get; }

        /// <summary>
        /// 現在の設定のパターンへの追加
        /// </summary>
        public ReactiveCommand AddSettingCommand { get; } = new ReactiveCommand();

        /// <summary>
        /// 削除パターンか置換パターンか
        /// </summary>
        public bool IsDelete { get; }

        public CommonPatternViewModel(CommonPattern modelPattern, bool isDelete)
        {
            this.modelPattern = modelPattern;
            this.SampleDiff = AppExtention.CreateDiff(modelPattern.SampleInput, modelPattern.SampleOutput);

            this.IsDelete = isDelete;

            AddSettingCommand.Subscribe(() =>
                (IsDelete ? model.Setting.DeleteTexts : model.Setting.ReplaceTexts)
                .Add(modelPattern.ToReplacePattern()));
        }
    }
}