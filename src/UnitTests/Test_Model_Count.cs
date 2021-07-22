using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Xunit;
using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;
using Reactive.Bindings;
using System.Reactive.Linq;

using FileRenamerDiff.Models;

namespace UnitTests
{
    public class Test_Model_Count
    {
        const string targetDirPath = @"D:\FileRenamerDiff_Test";
        const string SubDirName = "D_SubDir";
        static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
        static readonly string filePathB = Path.Combine(targetDirPath, "B.txt");
        static readonly string filePathC = Path.Combine(targetDirPath, "C.txt");
        static readonly string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
        static readonly string filePathE = Path.Combine(targetDirPath, SubDirName, "E.txt");
        static readonly string filePathF = Path.Combine(targetDirPath, SubDirName, "F.txt");

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
