using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Xunit;
using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Reactive.Bindings;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;

namespace UnitTests
{
    public class Test_MainWindowViewModel
    {
        private const string targetDirPath = @"D:\FileRenamerDiff_Test";
        private const string targetDirPathSub = @"D:\FileRenamerDiff_TestSub";
        private static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
        private static readonly string filePathB = Path.Combine(targetDirPath, "B.csv");

        [Fact]
        public async Task Test_Idle()
        {
            var fileDict = new[] { filePathA, filePathB }
               .ToDictionary(
                   s => s,
                   s => new MockFileData("mock"));
            var model = new MainModel(new MockFileSystem(fileDict));
            var mainVM = new MainWindowViewModel(model);

            mainVM.IsIdle.Value
                .Should().BeFalse(because: "起動中のはず");

            mainVM.Initialize();
            await Task.Delay(100);
            mainVM.IsIdle.Value
                .Should().BeTrue(because: "起動完了後のはず");

            Task<bool> isIdleTask = mainVM.IsIdle.WaitUntilValueChangedAsync();

            mainVM.LoadFilesFromCurrentPathCommand.Execute();

            await isIdleTask.Timeout(1000);

            mainVM.IsIdle.Value
                .Should().BeFalse(because: "ファイル読み込み中のはず");

            await mainVM.IsIdle.Where(x => x).FirstAsync().ToTask().Timeout(1000d);

            mainVM.IsIdle.Value
                .Should().BeTrue(because: "ファイル読み込み完了後のはず");
        }

        [Fact]
        public async Task Test_WindowTitle()
        {
            var fileDict = new[] { filePathA, filePathB }
                .ToDictionary(
                    s => s,
                    s => new MockFileData("mock"));

            var fileSystem = new MockFileSystem(fileDict);
            var model = new MainModel(fileSystem);
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            var mainVM = new MainWindowViewModel(model);

            mainVM.WindowTitle.Value
                .Should().Contain("FILE RENAMER DIFF", because: "アプリケーション名がWindowTitleに表示されるはず");
            mainVM.WindowTitle.Value
                .Should().NotContain(targetDirPath, because: "まだ読み取り先ファイルパスは表示されないはず");

            mainVM.Initialize();

            {
                Task taskWindowTitle = mainVM.WindowTitle.WaitUntilValueChangedAsync();
                mainVM.LoadFilesFromCurrentPathCommand.Execute();
                await taskWindowTitle.Timeout(1000d);
                await Task.Delay(10);
            }

            mainVM.WindowTitle.Value
                    .Should().Contain(targetDirPath, because: "読み取り先ファイルパスが表示されるはず");

            model.Setting.SearchFilePaths = new[] { targetDirPath, targetDirPathSub };

            {
                Task taskWindowTitle = mainVM.WindowTitle.WaitUntilValueChangedAsync();
                mainVM.LoadFilesFromCurrentPathCommand.Execute();
                await taskWindowTitle.Timeout(1000d);
                await Task.Delay(10);
            }

            mainVM.WindowTitle.Value
                .Should().Contain(targetDirPath, because: "読み取り先ファイルパスが表示されるはず");
            mainVM.WindowTitle.Value
                .Should().Contain(targetDirPathSub, because: "読み取り先ファイルパスが表示されるはず");

            {
                Task taskWindowTitle = mainVM.WindowTitle.WaitUntilValueChangedAsync();
                mainVM.GridVM.ClearFileElementsCommand.Execute();
                await taskWindowTitle.Timeout(1000d);
                await Task.Delay(10);
            }

            mainVM.WindowTitle.Value
                .Should().NotContain(targetDirPath, because: "読み取り先ファイルパスは表示されないはず");
        }

        [Fact]
        public async Task Test_Dialog()
        {
            var fileDict = new[] { filePathA, filePathB }
                .ToDictionary(
                    s => s,
                    s => new MockFileData("mock"));

            var fileSystem = new MockFileSystem(fileDict);
            var model = new MainModel(fileSystem);
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            var mainVM = new MainWindowViewModel(model);

            mainVM.Initialize();

            mainVM.LoadFilesFromCurrentPathCommand.Execute();
            await mainVM.IsIdle.WaitUntilValueChangedAsync();

            mainVM.DialogContentVM.Value.IsDialogOpen.Value
                .Should().BeFalse("正常にファイルを探索できたら、ダイアログは開いていないはず");

            model.Setting.SearchFilePaths = new[] { targetDirPathSub };

            mainVM.LoadFilesFromCurrentPathCommand.Execute();

            await Task.Delay(MainWindowViewModel.TimeSpanMessageBuffer * 2);

            mainVM.DialogContentVM.Value.IsDialogOpen.Value
                .Should().BeTrue("無効なファイルパスなら、ダイアログが開いたはず");

            (mainVM.DialogContentVM.Value as MessageDialogViewModel)?.AppMessage.MessageLevel
                .Should().Be(AppMessageLevel.Alert, "警告メッセージが表示されるはず");

        }
    }
}
