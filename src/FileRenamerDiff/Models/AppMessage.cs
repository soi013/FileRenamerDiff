namespace FileRenamerDiff.Models;

/// <summary>
/// アプリケーション内メッセージ
/// </summary>
public record AppMessage
{
    /// <summary>
    /// メッセージレベル
    /// </summary>
    public AppMessageLevel MessageLevel { get; init; }

    /// <summary>
    /// メッセージタイトル
    /// </summary>
    public string MessageHead { get; init; }

    /// <summary>
    /// メッセージ本体
    /// </summary>
    public string MessageBody { get; init; }

    public AppMessage(AppMessageLevel level, string head, string body = "")
    {
        MessageLevel = level;
        MessageHead = head;
        MessageBody = body;
    }
}

/// <summary>
/// アプリケーション内メッセージレベル
/// </summary>
public enum AppMessageLevel
{
    Info,
    Alert,
    Error
}

public static class AppMessageExt
{
    /// <summary>
    /// 同じヘッダのメッセージをまとめる
    /// </summary>
    public static IEnumerable<AppMessage> SumSameHead(this IEnumerable<AppMessage> messages)
    {
        AppMessage currentMessage = messages.First();
        var stbBody = new StringBuilder();
        stbBody.AppendLine(currentMessage.MessageBody);

        foreach (var m in messages.Skip(1))
        {
            if (currentMessage.MessageHead == m.MessageHead)
            {
                stbBody.AppendLine(m.MessageBody);
            }
            else
            {
                yield return CreateMessage(currentMessage, stbBody);
                stbBody.Clear();
                currentMessage = m;
                stbBody.AppendLine(m.MessageBody);
            }
        }

        yield return CreateMessage(currentMessage, stbBody);

        static AppMessage CreateMessage(AppMessage baseMessage, StringBuilder stbBody) =>
            baseMessage with { MessageBody = stbBody.ToString().TrimEnd('\r', '\n') };
    }
}
