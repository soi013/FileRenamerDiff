namespace FileRenamerDiff.Models;

/// <summary>
/// アプリケーション内メッセージ
/// </summary>
public class AppMessage
{
    /// <summary>
    /// メッセージレベル
    /// </summary>
    public AppMessageLevel MessageLevel { get; }

    /// <summary>
    /// メッセージタイトル
    /// </summary>
    public string MessageHead { get; }

    /// <summary>
    /// メッセージ本体
    /// </summary>
    public string MessageBody { get; }

    public AppMessage(AppMessageLevel level, string head, string body = "")
    {
        MessageLevel = level;
        MessageHead = head;
        MessageBody = body;
    }

    public override string ToString() => $"{MessageLevel}_{MessageHead}_({MessageBody})";
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

        static AppMessage CreateMessage(AppMessage currentMessage, StringBuilder stbBody) =>
            new(
                    currentMessage.MessageLevel,
                    currentMessage.MessageHead,
                    stbBody.ToString().TrimEnd('\r', '\n'));
    }
}
