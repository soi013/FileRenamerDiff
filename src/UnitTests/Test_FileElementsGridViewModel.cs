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
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;

using FluentAssertions;

using Reactive.Bindings;

using Xunit;

namespace UnitTests
{
    public class Test_FileElementsGridViewModel
    {
        private const string targetDirPath = @"D:\FileRenamerDiff_Test";
        private const string SubDirName = "D_SubDir";
        private static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
        private static readonly string filePathB = Path.Combine(targetDirPath, "B.txt");
        private static readonly string filePathC = Path.Combine(targetDirPath, "C.txt");
        private static readonly string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
        private static readonly string filePathE = Path.Combine(targetDirPath, SubDirName, "E.txt");
        private static readonly string filePathF = Path.Combine(targetDirPath, SubDirName, "F.txt");

        private static MockFileSystem CreateMockFileSystem()
        {
            return new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [filePathA] = new MockFileData("A"),
                [filePathB] = new MockFileData("B"),
                [filePathC] = new MockFileData("C"),
                [filePathDSubDir] = new MockDirectoryData(),
                [filePathE] = new MockFileData("E"),
                [filePathF] = new MockFileData("F"),
            });
        }
        private static MainModel CreateDefaultSettingModel()
        {
            MockFileSystem fileSystem = CreateMockFileSystem();

            var model = new MainModel(fileSystem);
            model.Initialize();
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            return model;
        }

        [Fact]
        public async Task Test_CountZero()
        {
            var scheduler = new HistoricalScheduler();
            ReactivePropertyScheduler.SetDefault(scheduler);
            var model = CreateDefaultSettingModel();

            await model.LoadFileElements();
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            var fileElementVMs = new FileElementsGridViewModel(model);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            fileElementVMs.IsAnyFiles.Value
                .Should().BeTrue("ファイル読込後なので、ファイルはあるはず");

            var cViewFileElementVMs = (ListCollectionView)fileElementVMs.CViewFileElementVMs;
            cViewFileElementVMs.IsEmpty
                .Should().BeFalse("ファイル読込後なので、ファイルはあるはず");
            cViewFileElementVMs.Count
                .Should().Be(6, "ファイル読込後なので、ファイルはあるはず");

            fileElementVMs.CountReplaced.Value
                .Should().Be(0, "置換前はまだ0のはず");

            fileElementVMs.CountConflicted.Value
                .Should().Be(0, "置換前はまだ0のはず");

            await model.Replace();

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            await Task.Delay(200);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            fileElementVMs.CountReplaced.Value
                .Should().Be(0, "置換する設定がないので、0のはず");
            fileElementVMs.CountConflicted.Value
                .Should().Be(0, "置換する設定がないので、0のはず");

            fileElementVMs.IsReplacedAny.Value
                .Should().BeFalse("置換する設定がないので、0のはず");
            fileElementVMs.IsNotConflictedAny.Value
                .Should().BeTrue("置換する設定がないので、0のはず");
        }

        [Fact]
        public async Task Test_CountNoConflict()
        {
            var scheduler = new HistoricalScheduler();
            ReactivePropertyScheduler.SetDefault(scheduler);
            var model = CreateDefaultSettingModel();

            await model.LoadFileElements();
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            var fileElementVMs = new FileElementsGridViewModel(model);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            var cViewFileElementVMs = (ListCollectionView)fileElementVMs.CViewFileElementVMs;

            model.Setting.ReplaceTexts.Add(new("B", "BBB"));
            model.Setting.ReplaceTexts.Add(new("C", "CCC"));
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            await model.Replace();

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));


            fileElementVMs.CountReplaced.Value
                .Should().Be(2, "置換する設定があるので、2のはず");
            fileElementVMs.CountConflicted.Value
                .Should().Be(0, "衝突はしないので、0のはず");

            fileElementVMs.IsReplacedAny.Value
                .Should().BeTrue("置換する設定があるので");
            fileElementVMs.IsNotConflictedAny.Value
                .Should().BeTrue("衝突はしないので");

            fileElementVMs.IsVisibleReplacedOnly.Value = true;
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            await Task.Delay(200);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            cViewFileElementVMs.Count
                .Should().Be(2, "フィルタ後は絞られたはず");

            fileElementVMs.IsVisibleConflictedOnly.Value = false;
            fileElementVMs.IsVisibleReplacedOnly.Value = false;
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            await Task.Delay(200);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            cViewFileElementVMs.Count
                .Should().Be(6, "フィルタ削除後はすべてのファイルがあるはず");
        }

        [Fact]
        public async Task Test_CountConflict()
        {
            var scheduler = new HistoricalScheduler();
            ReactivePropertyScheduler.SetDefault(scheduler);
            var model = CreateDefaultSettingModel();

            await model.LoadFileElements();
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            var fileElementVMs = new FileElementsGridViewModel(model);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            fileElementVMs.IsAnyFiles.Value
                .Should().BeTrue("ファイル読込後なので、ファイルはあるはず");

            var cViewFileElementVMs = (ListCollectionView)fileElementVMs.CViewFileElementVMs;

            model.Setting.ReplaceTexts.Add(new("B", "A"));
            model.Setting.ReplaceTexts.Add(new("C", "A"));

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            await model.Replace();

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            fileElementVMs.CountReplaced.Value
                .Should().Be(2, "置換する設定があるので、2のはず");
            fileElementVMs.CountConflicted.Value
                .Should().Be(3, "衝突するので、3のはず");

            fileElementVMs.IsReplacedAny.Value
                .Should().BeTrue("置換する設定があるので");
            fileElementVMs.IsNotConflictedAny.Value
                .Should().BeFalse("衝突はしないので");

            fileElementVMs.IsVisibleConflictedOnly.Value = true;
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            await Task.Delay(200);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            cViewFileElementVMs.Count
                .Should().Be(3, "フィルタ後は絞られたはず");

            fileElementVMs.IsVisibleReplacedOnly.Value = true;
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            await Task.Delay(200);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            cViewFileElementVMs.Count
                .Should().Be(2, "フィルタ後は絞られたはず");


            fileElementVMs.IsVisibleConflictedOnly.Value = false;
            fileElementVMs.IsVisibleReplacedOnly.Value = false;
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));
            await Task.Delay(200);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000));

            cViewFileElementVMs.Count
                .Should().Be(6, "フィルタ削除後はすべてのファイルがあるはず");
        }
    }
}
