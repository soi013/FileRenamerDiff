using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using Anotar.Serilog;
using Livet;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace FileRenamerDiff
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SetupLoggerConfig();
            DispatcherHelper.UIDispatcher = Dispatcher;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            LogTo.Information("App Start");
        }

        //Application level error handling
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                LogTo.Fatal(ex, "UnhandledException was occurred");

            Log.CloseAndFlush();

            MessageBox.Show(
                "Something errors were occurred.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Environment.Exit(1);
        }

        /// <summary>
        /// ロガーのセットアップ
        /// </summary>
        private static void SetupLoggerConfig()
        {
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
                            .WriteTo.File($"{logFilePathHead}.txt", LogEventLevel.Information, outputTemplate: template, rollingInterval: RollingInterval.Day)
                            //Comact-JSON型式のファイル出力を追加
                            .WriteTo.File(new CompactJsonFormatter(), $"{logFilePathHead}_comapct.json", LogEventLevel.Information, rollingInterval: RollingInterval.Day)
                            .CreateLogger();

            Thread.CurrentThread.Name = "UI";
        }
    }
}