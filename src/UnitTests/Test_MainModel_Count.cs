using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using FileRenamerDiff.Models;

using FluentAssertions;

using Reactive.Bindings;

using Xunit;

namespace UnitTests
{
    public class Test_MainModel_Count
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
            MainModel model = CreateDefaultSettingModel();

            await model.LoadFileElements();

            var messages = model.MessageEventStream.ToReactiveCollection();

            model.CountReplaced.Value
                .Should().Be(0, "置換前はまだ0のはず");

            model.CountConflicted.Value
                .Should().Be(0, "置換前はまだ0のはず");

            await model.Replace();

            model.CountReplaced.Value
                .Should().Be(0, "置換する設定がないので、0のはず");

            model.CountConflicted.Value
                .Should().Be(0, "置換する設定がないので、0のはず");

            messages
                .Should().HaveCount(0, "衝突はしないので、0のはず");
        }

        [Fact]
        public async Task Test_CountNoConflict()
        {
            MainModel model = CreateDefaultSettingModel();

            await model.LoadFileElements();

            model.Setting.ReplaceTexts.Add(new("B", "BBB"));
            model.Setting.ReplaceTexts.Add(new("C", "CCC"));

            var messages = model.MessageEventStream.ToReactiveCollection();

            await model.Replace();

            model.CountReplaced.Value
                .Should().Be(2, "置換する設定があるので、2のはず");

            model.CountConflicted.Value
                .Should().Be(0, "衝突はしないので、0のはず");

            messages
                .Should().HaveCount(0, "衝突はしないので、0のはず");
        }

        [Fact]
        public async Task Test_CountConflict()
        {
            MainModel model = CreateDefaultSettingModel();

            await model.LoadFileElements();

            model.Setting.ReplaceTexts.Add(new("B", "A"));
            model.Setting.ReplaceTexts.Add(new("C", "A"));

            var messages = new List<AppMessage>();

            model.MessageEventStream
                .Subscribe(x =>
                    messages.Add(x));

            await model.Replace();

            model.CountReplaced.Value
                .Should().Be(2, "置換する設定があるので、2のはず");

            model.CountConflicted.Value
                .Should().Be(3, "衝突するので、3のはず");

            messages.First().MessageLevel
                .Should().Be(AppMessageLevel.Alert, "ヘッダに警告があるはず");

            messages
                .Should().HaveCount(1, "衝突した場合はメッセージがあるはず");
        }
    }
}
