using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
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
        [InlineData(true)]
        [InlineData(false)]
        public async Task Test_ReplaceLogByEnableSetting(bool enableLog)
        {
            const string targetDirPath = @"D:\FileRenamerDiff_Test";
            string filePathA = Path.Combine(targetDirPath, "A.txt");
            string filePathB = Path.Combine(targetDirPath, "B.csv");

            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [filePathA] = new MockFileData("A"),
                [filePathB] = new MockFileData("B"),
            });

            var model = new MainModel(fileSystem);
            model.Initialize();
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            model.Setting.IsCreateRenameLog = enableLog;
            model.Setting.ReplaceTexts.Add(new ReplacePattern("A", "X"));
            await model.LoadFileElements();

            await model.Replace();
            await model.RenameExcute();

            if (!enableLog)
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
