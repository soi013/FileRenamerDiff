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

using FileRenamerDiff.Models;
using FileRenamerDiff.Properties;

namespace FileRenamerDiff.ViewModels
{
    public class ReplacePatternViewModel : ViewModel
    {
        private readonly ReplacePattern replacePattern;

        /// <summary>
        /// 置換される対象のパターン
        /// </summary>
        public ReactiveProperty<string> TargetPattern { get; }

        /// <summary>
        /// 置換後文字列
        /// </summary>
        public ReactiveProperty<string> ReplaceText { get; }

        /// <summary>
        /// パターンを単純一致か正規表現とするか
        /// </summary>
        public ReactiveProperty<bool> AsExpression { get; }

        public ReplacePatternViewModel(ReplacePattern replacePattern)
        {
            this.replacePattern = replacePattern;

            AsExpression = replacePattern
                .ToReactivePropertyAsSynchronized(x => x.AsExpression)
                .AddTo(this.CompositeDisposable);


            TargetPattern = replacePattern
                .ToReactivePropertyAsSynchronized(x => x.TargetPattern, mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
                .SetValidateNotifyError(x => AppExtention.IsValidRegexPattern(x,AsExpression.Value) ? null : "Invalid Pattern")
                .AddTo(this.CompositeDisposable);

            ReplaceText = replacePattern
                .ToReactivePropertyAsSynchronized(x => x.ReplaceText)
                .AddTo(this.CompositeDisposable);

            AsExpression
                .Subscribe(x => TargetPattern.ForceValidate());
        }

        /// <summary>
        /// ReplacePatternの取り出し
        /// </summary>
        public ReplacePattern ToReplacePattern() => replacePattern;

        public override string ToString() => replacePattern.ToString();
    }
}