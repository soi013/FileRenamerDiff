using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Anotar.Serilog;
using Livet;
using Livet.Messaging;
using Reactive.Bindings;
using Serilog.Events;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// アプリケーション内メッセージ
    /// </summary>
    public class AppMessage : NotificationObject
    {
        /// <summary>
        /// メッセージレベル
        /// </summary>
        public AppMessageLevel MessageLevel { get; set; }

        /// <summary>
        /// メッセージタイトル
        /// </summary>
        public string MessageHead { get; set; }

        /// <summary>
        /// メッセージ本体
        /// </summary>
        public string MessageBody { get; set; }

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
                    currentMessage.MessageBody = stbBody.ToString();
                    yield return currentMessage;
                    stbBody.Clear();
                    currentMessage = m;
                    stbBody.AppendLine(m.MessageBody);
                }
            }
            currentMessage.MessageBody = stbBody.ToString();
            yield return currentMessage;
        }
    }
}