﻿using Livet;

namespace FileRenamerDiff.Models;

/// <summary>
/// 置換前後のパターン
/// </summary>
public class ReplacePattern : NotificationObject
{
    private string _TargetPattern;
    /// <summary>
    /// 置換される対象のパターン
    /// </summary>
    public string TargetPattern
    {
        get => _TargetPattern;
        set => RaisePropertyChangedIfSet(ref _TargetPattern, value);
    }

    private string _ReplaceText;
    /// <summary>
    /// 置換後文字列
    /// </summary>
    public string ReplaceText
    {
        get => _ReplaceText;
        set => RaisePropertyChangedIfSet(ref _ReplaceText, value);
    }

    private bool _AsExpression;
    /// <summary>
    /// パターンを単純一致か正規表現とするか
    /// </summary>
    public bool AsExpression
    {
        get => _AsExpression;
        set => RaisePropertyChangedIfSet(ref _AsExpression, value);
    }

    /// <summary>
    /// 置換パターンを組み立てる
    /// </summary>
    /// <param name="targetPattern">置換される対象のパターン</param>
    /// <param name="replaceText">置換後文字列</param>
    /// <param name="asExpression">パターンを単純一致か正規表現とするか</param>
    public ReplacePattern(string targetPattern, string replaceText, bool asExpression = false)
    {
        this._TargetPattern = targetPattern;
        this._AsExpression = asExpression;
        this._ReplaceText = replaceText;
    }

    public ReplaceRegexBase? ToReplaceRegex()
    {
        var patternEx = AsExpression
            ? TargetPattern
            : Regex.Escape(TargetPattern);

        //無効なパターンの場合はnullになる
        Regex? regex = AppExtension.CreateRegexOrNull(patternEx);

        return regex == null ? null
            : AddDirectoryNameRegex.IsContainPattern(ReplaceText) ? new AddDirectoryNameRegex(regex, ReplaceText)
            : AddSerialNumberRegex.IsContainPattern(ReplaceText) ? new AddSerialNumberRegex(regex, ReplaceText)
            : AddTimeRegex.IsContainPattern(ReplaceText) ? new AddTimeRegex(regex, ReplaceText)
            : new ReplaceRegex(regex, ReplaceText);
    }

    internal static ReplacePattern CreateEmpty() => new(string.Empty, string.Empty);

    public override string ToString() => $"{TargetPattern}->{ReplaceText}";
}
