﻿using System.Reactive.Linq;

using FileRenamerDiff.Models;

using Livet;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace FileRenamerDiff.ViewModels;

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
            .ToReactivePropertyAsSynchronized(
                x => x.TargetPattern,
                mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError,
                ignoreValidationErrorValue: true)
            .SetValidateNotifyError(x => AppExtension.IsValidRegexPattern(x, AsExpression.Value) ? null : "Invalid Pattern")
            .AddTo(this.CompositeDisposable);

        ReplaceText = replacePattern
            .ToReactivePropertyAsSynchronized(
                x => x.ReplaceText,
                mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError,
                ignoreValidationErrorValue: true)
            .SetValidateNotifyError(x => AppExtension.IsValidReplacePattern(x, AsExpression.Value) ? null : "Invalid Pattern")
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
