using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Anotar.Serilog;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;

using FluentAssertions;

using Reactive.Bindings;

using Xunit;

namespace UnitTests
{
    public class Test_MainWindowViewModel : IClassFixture<LogFixture>
    {
        private const string targetDirPath = @"D:\FileRenamerDiff_Test";
        private const string targetDirPathSub = @"D:\FileRenamerDiff_TestSub";
        private const string fileNameA = "A.txt";
        private const string fileNameB = "B.csv";
        private static readonly string filePathA = Path.Combine(targetDirPath, fileNameA);
        private static readonly string filePathB = Path.Combine(targetDirPath, fileNameB);

        [WpfFact]
        public async Task Test_Idle()
        {
            var fileDict = new[] { filePathA, filePathB }
               .ToDictionary(
                   s => s,
                   s => new MockFileData("mock"));
            var model = new MainModel(new MockFileSystem(fileDict), Scheduler.Immediate);
            var mainVM = new MainWindowViewModel(model);

            mainVM.IsIdle.Value
                .Should().BeFalse(because: "起動中のはず");

            mainVM.Initialize();
            await mainVM.IsIdle
                .WaitShouldBe(true, 3000d, "起動完了後のはず");

            model.Setting.SearchFilePaths = new[] { targetDirPath };
            await mainVM.LoadFilesFromCurrentPathCommand.ExecuteAsync().Timeout(10000d);

            await mainVM.IsIdle
               .WaitShouldBe(true, 3000d, "ファイル読み込み完了後のはず");
        }

        [WpfFact]
        public async Task Test_WindowTitle()
        {
            var fileDict = new[] { filePathA, filePathB }
                .ToDictionary(
                    s => s,
                    s => new MockFileData("mock"));

            var fileSystem = new MockFileSystem(fileDict);
            var syncScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
            var model = new MainModel(fileSystem, syncScheduler);
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            var mainVM = new MainWindowViewModel(model);
            mainVM.WindowTitle.Subscribe(x =>
                LogTo.Information("Window title is {@title}", x));

            mainVM.WindowTitle.Value
                .Should().Contain("FILE RENAMER DIFF", because: "アプリケーション名がWindowTitleに表示されるはず");
            mainVM.WindowTitle.Value
                .Should().NotContain(targetDirPath, because: "まだ読み取り先ファイルパスは表示されないはず");

            mainVM.Initialize();

            await mainVM.WaitIdle().Timeout(3000d);

            await mainVM.LoadFilesFromCurrentPathCommand.ExecuteAsync();

            await mainVM.WindowTitle
                .WaitShouldBe(x => x.Contains(targetDirPath), 3000d, because: "読み取り先ファイルパスが表示されるはず");

            model.Setting.SearchFilePaths = new[] { targetDirPath, targetDirPathSub };

            await mainVM.LoadFilesFromCurrentPathCommand.ExecuteAsync();

            await mainVM.WindowTitle
                .WaitShouldBe(x => x.Contains(targetDirPath) && x.Contains(targetDirPathSub), 3000d, because: "読み取り先ファイルパスが表示されるはず");

            {
                Task taskWindowTitle = mainVM.WindowTitle.WaitUntilValueChangedAsync();
                mainVM.GridVM.ClearFileElementsCommand.Execute();
                await taskWindowTitle.Timeout(10000d);
                await Task.Delay(10);
            }

            mainVM.WindowTitle.Value
                .Should().NotContain(targetDirPath, because: "読み取り先ファイルパスは表示されないはず");
        }

        [WpfFact]
        public async Task Test_Dispose()
        {
            var fileSystem = new MockFileSystem();
            var model = new MainModel(fileSystem, Scheduler.Immediate);
            var mainVM = new MainWindowViewModel(model);
            mainVM.Initialize();
            await Task.Delay(10);
            await mainVM.IsIdle.Where(x => x).FirstAsync().ToTask().Timeout(10000d);

            const string newIgnoreExt = "newignoreext";
            mainVM.SettingVM.Value.IgnoreExtensions.Clear();
            mainVM.SettingVM.Value.IgnoreExtensions.Add(new(newIgnoreExt));

            mainVM.Dispose();

            fileSystem.File.ReadAllText(SettingAppModel.DefaultFilePath)
                .Should().Contain(newIgnoreExt, because: "設定ファイルの中身に新しい設定値が保存されているはず");
        }

        [WpfFact]
        public async Task Test_CommandCanExecute()
        {
            var fileDict = new[] { filePathA, filePathB }
                .ToDictionary(
                    s => s,
                    s => new MockFileData("mock"));

            var fileSystem = new MockFileSystem(fileDict);
            var syncScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
            var model = new MainModel(fileSystem, syncScheduler);
            var mainVM = new MainWindowViewModel(model);
            mainVM.Initialize();
            await mainVM.WaitIdle().Timeout(3000d);
            await Task.Delay(100);

            //ステージ1 初期状態
            var canExecuteUsuallyCommand = new ICommand[]
            {
                mainVM.AddFilesFromDialogCommand,
                mainVM.LoadFilesFromCurrentPathCommand,
                mainVM.LoadFilesFromDialogCommand,
                mainVM.LoadFilesFromNewPathCommand,
                mainVM.ShowHelpPageCommand,
                mainVM.ShowInformationPageCommand,
            };

            for (int i = 0; i < canExecuteUsuallyCommand.Length; i++)
            {
                canExecuteUsuallyCommand[i]
                    .CanExecute(null)
                    .Should().BeTrue($"すべて実行可能はなず (indexCommand:{i})");
            }

            mainVM.ReplaceCommand.CanExecute()
                .Should().BeFalse("実行不可能のはず");

            //ステージ2 ファイル読み込み中
            Task<bool> isIdleTask = mainVM.IsIdle.WaitUntilValueChangedAsync();
            mainVM.LoadFilesFromNewPathCommand.Execute(new[] { targetDirPath });
            await isIdleTask.Timeout(10000d);

            //CI上ではなぜか失敗することもある
            //canExecuteUsuallyCommand
            //    .Concat(new[] { mainVM.ReplaceCommand })
            //    .ForEach((c, i) =>
            //        c.CanExecute(null)
            //        .Should().BeFalse($"すべて実行不可能はなず (indexCommand:{i})"));

            //ステージ2 ファイル読み込み後
            await mainVM.WaitIdle().Timeout(3000);
            await Task.Delay(100);
            canExecuteUsuallyCommand
                .Concat(new[] { mainVM.ReplaceCommand })
                .ForEach((c, i) =>
                    c.CanExecute(null)
                    .Should().BeTrue($"すべて実行可能はなず (indexCommand:{i})"));

            mainVM.RenameExecuteCommand.CanExecute()
                .Should().BeFalse("実行不可能のはず");

            //ステージ3 重複したリネーム後
            var replaceConflict = new ReplacePattern(fileNameA, fileNameB);
            mainVM.SettingVM.Value.ReplaceTexts.Add(new ReplacePatternViewModel(replaceConflict));
            await mainVM.ReplaceCommand.ExecuteAsync();

            await mainVM.WaitIdle().Timeout(3000);
            await Task.Delay(10);

            mainVM.RenameExecuteCommand.CanExecute()
                .Should().BeFalse("まだ実行不可能のはず");

            //ステージ4 なにも変化しないリネーム後
            mainVM.SettingVM.Value.ReplaceTexts.Clear();
            await mainVM.ReplaceCommand.ExecuteAsync();

            await mainVM.WaitIdle().Timeout(3000);
            await Task.Delay(10);

            mainVM.RenameExecuteCommand.CanExecute()
                .Should().BeFalse($"まだ実行不可能のはず。IsIdle:{mainVM.IsIdle.Value}, CountConflicted:{model.CountConflicted.Value}, CountReplaced:{model.CountReplaced.Value}");

            //ステージ5 有効なネーム後
            var replaceSafe = new ReplacePattern(fileNameA, "XXX_" + fileNameA);
            var replaceSafeVM = new ReplacePatternViewModel(replaceSafe);
            replaceSafeVM.ToString()
                .Should().ContainAll(fileNameA, "XXX_");
            mainVM.SettingVM.Value.ReplaceTexts.Add(replaceSafeVM);
            await mainVM.ReplaceCommand.ExecuteAsync();

            await mainVM.WaitIdle().Timeout(3000);
            await Task.Delay(10);

            mainVM.RenameExecuteCommand.CanExecute()
                .Should().BeTrue($"実行可能になったはず。IsIdle:{mainVM.IsIdle.Value}, CountConflicted:{model.CountConflicted.Value}, CountReplaced:{model.CountReplaced.Value}");

            //ステージ6 リネーム保存後
            await mainVM.RenameExecuteCommand.ExecuteAsync();

            await mainVM.WaitIdle().Timeout(3000);
            await Task.Delay(10);

            canExecuteUsuallyCommand
              .Concat(new[] { mainVM.ReplaceCommand })
                .ForEach((c, i) =>
                    c.CanExecute(null)
                    .Should().BeTrue($"すべて実行可能はなず (indexCommand:{i})"));

            mainVM.RenameExecuteCommand.CanExecute()
                .Should().BeFalse($"実行不可能に戻ったはず。IsIdle:{mainVM.IsIdle.Value}, CountConflicted:{model.CountConflicted.Value}, CountReplaced:{model.CountReplaced.Value}");
        }

        [WpfFact]
        public async Task Test_ClearSettingAndConfirm()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
            var mainVM = new MainWindowViewModel(model);

            model.Initialize();
            await model.WaitIdleUI().Timeout(3000d);

            SettingAppViewModel settingVM = mainVM.SettingVM.Value;

            //ステージ 初期状態
            settingVM.IgnoreExtensions.Count
                .Should().BeGreaterThan(1, "設定がなにかあるはず");

            //ステージ 設定消去→確認ダイアログ表示
            Task taskClear1 = settingVM.ClearIgnoreExtensionsCommand.ExecuteAsync();

            mainVM.IsDialogOpen.Value
                .Should().BeTrue("確認ダイアログが開いているはず");

            mainVM.DialogContentVM.Value
               .Should().BeOfType<ConfirmDialogViewModel>("確認ダイアログが開いているはず");

            ConfirmDialogViewModel confirmVM = (mainVM.DialogContentVM.Value as ConfirmDialogViewModel)!;

            taskClear1.IsCompleted
                .Should().BeFalse("まだ確認ダイアログが開いたままのはず");

            //ステージ 確認ダイアログで拒否
            confirmVM.IsOkResult.Value = false;

            taskClear1.IsCompleted
                .Should().BeTrue("確認ダイアログが閉じたはず");
            mainVM.IsDialogOpen.Value
                .Should().BeFalse("確認ダイアログが閉じたはず");

            settingVM.IgnoreExtensions.Count
                .Should().BeGreaterThan(1, "設定はまだ消去されていないはず");

            //ステージ確認ダイアログでOK
            Task taskClear2 = settingVM.ClearIgnoreExtensionsCommand.ExecuteAsync();

            (mainVM.DialogContentVM.Value as ConfirmDialogViewModel)!.IsOkResult.Value = true;

            taskClear2.IsCompleted
                .Should().BeTrue("確認ダイアログが閉じたはず");
            mainVM.IsDialogOpen.Value
                .Should().BeFalse("確認ダイアログが閉じたはず");

            settingVM.IgnoreExtensions.Count
                .Should().Be(0, "設定が消去されたはず");
        }

        [WpfFact]
        public async Task Test_Dialog()
        {
            var fileDict = new[] { filePathA, filePathB }
                .ToDictionary(
                    s => s,
                    s => new MockFileData("mock"));

            var fileSystem = new MockFileSystem(fileDict);
            var model = new MainModel(fileSystem, Scheduler.Immediate);
            var mainVM = new MainWindowViewModel(model);

            mainVM.Initialize();
            await mainVM.WaitIdle().Timeout(3000d);

            var dialogMessage = new Livet.Messaging.IO.FolderSelectionMessage { Response = new[] { targetDirPath } };
            await mainVM.LoadFilesFromDialogCommand.ExecuteAsync(dialogMessage);

            await mainVM.WaitIdle().Timeout(3000d);
            await Task.Delay(MainWindowViewModel.TimeSpanMessageBuffer * 3);

            mainVM.IsDialogOpen.Value
                .Should().BeFalse("正常にファイルを探索できたら、ダイアログは開いていないはず");

            dialogMessage = new Livet.Messaging.IO.FolderSelectionMessage { Response = new[] { "*invalidPath1*", "*invalidPath2*" } };
            await mainVM.LoadFilesFromDialogCommand.ExecuteAsync(dialogMessage);

            await mainVM.IsDialogOpen.WaitShouldBe(true, 3000d, "無効なファイルパスなら、ダイアログが開いたはず");

            (mainVM.DialogContentVM.Value as MessageDialogViewModel)?.AppMessage.MessageLevel
                .Should().Be(AppMessageLevel.Alert, "警告メッセージが表示されるはず");
        }
    }
}
