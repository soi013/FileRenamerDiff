using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using FileRenamerDiff.Models;

using FluentAssertions;

using Xunit;

namespace UnitTests
{
    public class Test_ReplaceLog
    {
        [Theory]
        [InlineData(true, "A", true)]
        [InlineData(false, "A", false)]
        [InlineData(true, "X", false)]
        [InlineData(false, "X", false)]
        public async Task Test_ReplaceLogByEnableSetting(bool enableLog, string targetPattern, bool expectedResult)
        {
            const string targetDirPath = @"D:\FileRenamerDiff_Test";
            string filePathA = Path.Combine(targetDirPath, "A.txt");
            string filePathB = Path.Combine(targetDirPath, "B.csv");

            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [filePathA] = new MockFileData("A"),
                [filePathB] = new MockFileData("B"),
            });

            var model = new MainModel(fileSystem, Scheduler.Immediate);
            model.Initialize();
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            model.Setting.IsCreateRenameLog = enableLog;
            var rPattern = new ReplacePattern(targetPattern, "X");
            rPattern.ToString()
                .Should().ContainAll(targetPattern, "X");

            model.Setting.ReplaceTexts.Add(rPattern);
            await model.LoadFileElements();

            await model.Replace();
            await model.RenameExecute();

            if (!expectedResult)
            {
                fileSystem.AllFiles
                    .Should().NotContain("RenameLog", "ログ設定が無効ならログファイルはないはず");

                return;
            }

            string? logFilePath = fileSystem.AllFiles.Where(x => x.Contains("RenameLog")).FirstOrDefault();
            string? logContent = logFilePath is null
                ? null
                : fileSystem.File.ReadAllText(logFilePath);

            logContent
                .Should().Contain("A.txt", "リネームログがあるはず");
            logContent
                .Should().Contain("X.txt", "リネームログがあるはず");
        }
    }
}
