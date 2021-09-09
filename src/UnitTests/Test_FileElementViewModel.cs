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

using DiffPlex.DiffBuilder.Model;

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
    public class Test_FileElementViewModel : IClassFixture<LogFixture>
    {
        private const string targetDirPath = @"D:\FileRenamerDiff_Test";
        private const string fileNameA = "A.hoge";
        private static readonly string filePathA = Path.Combine(targetDirPath, fileNameA);
        private static readonly DateTime fileACreationTime = new(2020, 01, 02, 03, 45, 06);
        private static readonly DateTime fileAWriteTime = new(2021, 12, 11, 10, 09, 08);

        private static MockFileSystem CreateMockFileSystem()
        {
            return new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [SettingAppModel.DefaultFilePath] = new MockFileData(string.Empty),
                [filePathA] = new MockFileData("12345678")
                {
                    CreationTime = fileACreationTime,
                    LastWriteTime = fileAWriteTime,
                },
            });
        }

        [Fact]
        public void Test_FileProperties()
        {
            var fileSystem = CreateMockFileSystem();
            var messageEvent = new Subject<AppMessage>();
            var fileElementM = new FileElementModel(fileSystem, filePathA, messageEvent);
            var fileElementVM = new FileElementViewModel(fileElementM);

            fileElementVM.CreationTime
                .Should().Be(fileACreationTime.ToString());
            fileElementVM.LastWriteTime
                .Should().Be(fileAWriteTime.ToString());
            fileElementVM.DirectoryPath
                .Should().Be(targetDirPath);
            fileElementVM.LengthByte
                .Should().Be(8, "中の文字数が8文字なので");
            fileElementVM.Category
                .Should().Be(FileCategories.OtherFile);
            fileElementM.ToString()
                .Should().Contain(fileNameA);
        }

        [Theory]
        [InlineData("ABC.txt", "B", "Z", "ABC|.|txt", "AZC|.|txt")]
        [InlineData("Del.ete.txt", "ete", "", "Del|.|ete|.|txt", "Del|..|||txt")]
        [InlineData("fix tgt fix", "tgt", "REP", "fix| |tgt| |fix", "fix| |REP| |fix")]
        [InlineData("fix_tgt_fix", "tgt", "REP", "fix|_|tgt|_|fix", "fix|_|REP|_|fix")]
        [InlineData("fix,tgt,fix", "tgt", "REP", "fix|,|tgt|,|fix", "fix|,|REP|,|fix")]
        [InlineData("fix.tgt.fix", "tgt", "REP", "fix|.|tgt|.|fix", "fix|.|REP|.|fix")]
        [InlineData("fix -tgt -fix", "tgt", "REP", "fix| -|tgt| -|fix", "fix| -|REP| -|fix")]
        [InlineData("fix=tgt=fix", "tgt", "REP", "fix|=|tgt|=|fix", "fix|=|REP|=|fix")]
        [InlineData("fix　tgt　fix", "tgt", "REP", "fix|　|tgt|　|fix", "fix|　|REP|　|fix")]
        [InlineData("fix・tgt・fix", "tgt", "REP", "fix|・|tgt|・|fix", "fix|・|REP|・|fix")]
        [InlineData("fix／tgt／fix", "tgt", "REP", "fix|／|tgt|／|fix", "fix|／|REP|／|fix")]
        [InlineData("fix(tgt)fix", "tgt", "REP", "fix|(|tgt|)|fix", "fix|(|REP|)|fix")]
        [InlineData("fix[tgt]fix", "tgt", "REP", "fix|[|tgt|]|fix", "fix|[|REP|]|fix")]
        public void Test_Replace(string targetFileName, string regexPattern, string replaceText, string oldPiecedFileName, string newPiecedFileName)
        {
            string targetFilePath = Path.Combine(targetDirPath, targetFileName);
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [targetFilePath] = new MockDirectoryData()
            });
            var messageEvent = new Subject<AppMessage>();
            FileElementModel fileElementM = new(fileSystem, targetFilePath, messageEvent);
            FileElementViewModel fileElementVM = new(fileElementM);

            //ファイル名の一部を変更する置換パターンを作成
            ReplaceRegex[] replaceRegexes = new[]
                {
                    new ReplaceRegex(new Regex(regexPattern, RegexOptions.Compiled), replaceText)
                };

            //リネームプレビュー実行
            fileElementM.Replace(replaceRegexes, false);

            fileElementVM.IsReplaced.Value
                .Should().BeTrue("置換後のはず");

            SideBySideDiffModel diff = fileElementVM.Diff.Value!;

            diff.OldText.ToRawText()
                .Should().Be(targetFileName, because: "置換前の名前のはず");
            diff.OldText.Lines.First().SubPieces.Select(x => x.Text).ConcatenateString('|')
                .Should().Be(oldPiecedFileName, "ハイライト分割状態の古い文字列");
            diff.NewText.Lines.First().SubPieces.Select(x => x.Text).ConcatenateString('|')
                .Should().Be(newPiecedFileName, "ハイライト分割状態の新しい文字列");
        }
    }
}
