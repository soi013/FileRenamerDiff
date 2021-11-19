namespace FileRenamerDiff.Models;

/// <summary>
/// 処理状態情報
/// </summary>
/// <param name="Count">処理カウント</param>
/// <param name="Message">処理状態メッセージ</param>
public record ProgressInfo(int Count, string Message) { }
