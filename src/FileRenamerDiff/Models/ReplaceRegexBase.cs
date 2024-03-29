﻿using System.IO.Abstractions;

namespace FileRenamerDiff.Models;

/// <summary>
/// 正規表現を用いて文字列を置換する処理とパターンを保持するベースクラス
/// </summary>
public abstract class ReplaceRegexBase
{
    /// <summary>
    /// ターゲットを指定する正規表現
    /// </summary>
    protected readonly Regex regex;

    public ReplaceRegexBase(Regex regex)
    {
        this.regex = regex;
    }

    /// <summary>
    /// 置換実行
    /// </summary>
    /// <param name="input">入力文字列</param>
    /// <param name="allPaths">全ての入力ファイルパス（一部の継承する置換クラスで使用）</param>
    /// <param name="fsInfo">ファイル情報（一部の継承する置換クラスで使用）</param>
    /// <returns>置換後文字列</returns>
    internal abstract string Replace(string input, IReadOnlyList<string>? allPaths = null, IFileSystemInfo? fsInfo = null);
}
