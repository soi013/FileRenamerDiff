using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;

using Anotar.Serilog;

using Reactive.Bindings;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace UnitTests;

public class LogFixture
{
    public LogFixture()
    {
        SetupLoggerConfig();

        LogTo.Information("Fixture setuped");
    }

    private static void SetupLoggerConfig()
    {
        Thread.CurrentThread.Name ??= "CT";

        if (Log.Logger is Serilog.Core.Logger)
            return;

        //メッセージテンプレート 現在時刻、ログレベル、スレッドID・名称、メッセージ本文、呼び出し元名前空間＋クラス名、呼び出し元メソッドシグネチャ、行番号、使用メモリ量、(あれば例外)が保存される
        string template = "| {Timestamp:HH:mm:ss.fff} | {Level:u4} | {ThreadId:00}:{ThreadName} | {Message:j} | {SourceContext} | {MethodName} | {LineNumber} L | {MemoryUsage} B|{NewLine}{Exception}";

        string logFilePathHead = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            + $@"\{nameof(FileRenamerDiff)}\logs\{nameof(FileRenamerDiff)}";

        Log.Logger = new LoggerConfiguration()
                        .Enrich.WithThreadId()
                        //UIスレッドは"UI"それ以外は"__"と表示
                        .Enrich.WithThreadName().Enrich.WithProperty("ThreadName", "__")
                        .Enrich.WithMemoryUsage()
                        //例外種類・Message・StackTraceを表示
                        .Enrich.WithExceptionDetails()
                        .MinimumLevel.Verbose()
                        //上のテンプレートのメッセージをデバッグ出力とファイルに保存
                        .WriteTo.Debug(outputTemplate: template)
                        .CreateLogger();
    }
}
