using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;

using FluentAssertions;
using FluentAssertions.Extensions;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Schedulers;

using Xunit;

namespace UnitTests
{
    public class Test_FileElementsGridViewModel_AddRemove : IClassFixture<LogFixture>
    {
        private const string targetDirPath = @"D:\FileRenamerDiff_Test";
        private const string SubDirName = "D_SubDir";
        private static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
        private static readonly string filePathB = Path.Combine(targetDirPath, "B.txt");
        private static readonly string filePathC = Path.Combine(targetDirPath, "C.txt");
        private static readonly string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
        private static readonly string filePathE = Path.Combine(targetDirPath, SubDirName, "E.txt");
        private static readonly string filePathF = Path.Combine(targetDirPath, SubDirName, "F.txt");
        private static readonly string filePathG = Path.Combine(targetDirPath, SubDirName, "G.txt");

        private static MockFileSystem CreateMockFileSystem()
        {
            return new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [SettingAppModel.DefaultFilePath] = new MockFileData(string.Empty),
                [filePathA] = new MockFileData("A"),
                [filePathB] = new MockFileData("B"),
                [filePathC] = new MockFileData("C"),
                [filePathDSubDir] = new MockDirectoryData(),
                [filePathE] = new MockFileData("E"),
                [filePathF] = new MockFileData("F"),
                [filePathG] = new MockFileData("G") { Attributes = FileAttributes.Hidden },
            });
        }

        [WpfFact]
        public async Task Test_AddTargetFiles_RemoveFile()
        {
            MockFileSystem fileSystem = CreateMockFileSystem();
            var syncScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);

            var model = new MainModel(fileSystem, syncScheduler);
            model.Initialize();

            var fileElementVMs = new FileElementsGridViewModel(model);

            await model.WaitIdleUI().Timeout(3000);

            //ステージ ファイル追加後
            fileElementVMs.AddTargetFilesCommand.Execute(new[] { filePathA, filePathG });
            await model.WaitIdleUI().Timeout(3000);
            await Task.Delay(100);

            fileElementVMs.IsAnyFiles.Value
                .Should().BeTrue("ファイル読込後なので、ファイルはあるはず");

            //なぜかReactiveCollectionをつかったテストは複数のテストをまとめて実行するとエラーする
            //var cViewFileElementVMs = (ListCollectionView)fileElementVMs.CViewFileElementVMs;
            //await cViewFileElementVMs.ObserveProperty(x => x.Count)
            //    .WaitShouldBe(1, 3000d, "ファイル読込後なので、ファイルはあるはず");

            model.FileElementModels
                .Should().HaveCount(2, "ファイル読込後なので、ファイルはあるはず");
            fileElementVMs.fileElementVMs
                .Should().HaveCount(2, "ファイル読込後なので、ファイルはあるはず");

            await Task.Delay(100);

            //ステージ ファイル削除後
            var removedVM = fileElementVMs.fileElementVMs
                .First(x => x.PathModel.InputFilePath == filePathA);

            fileElementVMs.RemoveItemCommand.Execute(removedVM);
            await Task.Delay(100);

            model.FileElementModels.Count
                .Should().Be(1, "ファイル削除後なので、ファイルは減ったはず");

            await Task.Delay(100);

            //ステージ　ファイルクリア後
            fileElementVMs.ClearFileElementsCommand.Execute();
            await Task.Delay(100);

            model.FileElementModels.Count
                .Should().Be(0, "ファイルはなくなったはず");
        }
    }
}
