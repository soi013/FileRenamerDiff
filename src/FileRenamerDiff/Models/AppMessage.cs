using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Anotar.Serilog;
using Livet;
using MessagePack;
using MessagePack.ReactivePropertyExtension;
using MessagePack.Resolvers;
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
}