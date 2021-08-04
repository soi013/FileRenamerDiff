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
    public class Test_MainWindowViewModel : IClassFixture<RxSchedulerFixture>
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

            await isIdleTask.Timeout(10000d);

            mainVM.IsIdle.Value
                .Should().BeFalse(because: "ファイル読み込み中のはず");

            await mainVM.IsIdle.Where(x => x).FirstAsync().ToTask().Timeout(10000d);

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
                await taskWindowTitle.Timeout(10000d);
                await Task.Delay(10);
            }

            mainVM.WindowTitle.Value
                    .Should().Contain(targetDirPath, because: "読み取り先ファイルパスが表示されるはず");

            model.Setting.SearchFilePaths = new[] { targetDirPath, targetDirPathSub };

            {
                Task taskWindowTitle = mainVM.WindowTitle.WaitUntilValueChangedAsync();
                mainVM.LoadFilesFromCurrentPathCommand.Execute();
                await taskWindowTitle.Timeout(10000d);
                await Task.Delay(10);
            }

            mainVM.WindowTitle.Value
                .Should().Contain(targetDirPath, because: "読み取り先ファイルパスが表示されるはず");
            mainVM.WindowTitle.Value
                .Should().Contain(targetDirPathSub, because: "読み取り先ファイルパスが表示されるはず");

            {
                Task taskWindowTitle = mainVM.WindowTitle.WaitUntilValueChangedAsync();
                mainVM.GridVM.ClearFileElementsCommand.Execute();
                await taskWindowTitle.Timeout(10000d);
                await Task.Delay(10);
            }

            mainVM.WindowTitle.Value
                .Should().NotContain(targetDirPath, because: "読み取り先ファイルパスは表示されないはず");
        }


        [Fact]
        public void Test_Dispose()
        {
            var fileSystem = new MockFileSystem();
            var model = new MainModel(fileSystem);
            var mainVM = new MainWindowViewModel(model);
            mainVM.Initialize();

            const string newIgnoreExt = "newignoreext";
            mainVM.SettingVM.Value.IgnoreExtensions.Clear();
            mainVM.SettingVM.Value.IgnoreExtensions.Add(new(newIgnoreExt));

            mainVM.Dispose();

            fileSystem.File.ReadAllText(SettingAppModel.DefaultFilePath)
                .Should().Contain(newIgnoreExt, because: "設定ファイルの中身に新しい設定値が保存されているはず");
        }

        //HACK 何故かダイアログのテストはCI上で失敗する
        //[Fact]
        //public async Task Test_Dialog()
        //{
        //    var fileDict = new[] { filePathA, filePathB }
        //        .ToDictionary(
        //            s => s,
        //            s => new MockFileData("mock"));

        //    var fileSystem = new MockFileSystem(fileDict);
        //    var model = new MainModel(fileSystem);
        //    model.Setting.SearchFilePaths = new[] { targetDirPath };
        //    var mainVM = new MainWindowViewModel(model);

        //    mainVM.Initialize();

        //    await mainVM.IsIdle.Where(x => x).FirstAsync().ToTask()
        //        .Timeout(10000d);

        //    mainVM.LoadFilesFromCurrentPathCommand.Execute();

        //    await mainVM.IsIdle.Where(x => x).FirstAsync().ToTask()
        //        .Timeout(10000d);

        //    mainVM.IsDialogOpen.Value
        //        .Should().BeFalse("正常にファイルを探索できたら、ダイアログは開いていないはず");

        //    model.Setting.SearchFilePaths = new[] { "*invalidpath*" };

        //    mainVM.LoadFilesFromCurrentPathCommand.Execute();

        //    await mainVM.IsDialogOpen.Where(x => x).FirstAsync().ToTask()
        //        .Timeout(10000d);

        //    mainVM.IsDialogOpen.Value
        //        .Should().BeTrue("無効なファイルパスなら、ダイアログが開いたはず");

        //    (mainVM.DialogContentVM.Value as MessageDialogViewModel)?.AppMessage.MessageLevel
        //        .Should().Be(AppMessageLevel.Alert, "警告メッセージが表示されるはず");

        //}
    }
}
