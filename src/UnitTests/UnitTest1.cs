using System;
using Xunit;
using FileRenamerDiff.Models;
using System.Collections.Generic;
using FluentAssertions;
using System.Text.RegularExpressions;
using System.IO.Abstractions.TestingHelpers;
using System.IO;
using System.Linq;

namespace UnitTests
{
    public class UnitTest1
    {
        [Fact]
        public void Test_ValueHolder()
        {
            var queuePropertyChanged = new Queue<string?>();
            var holder = ValueHolderFactory.Create(string.Empty);

            holder.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

            holder.Value
                .Should().BeEmpty("初期値は空のはず");

            queuePropertyChanged
                .Should().BeEmpty("まだ通知は来ていないはず");

            const string newValue = "NEW_VALUE";
            holder.Value = newValue;

            holder.Value
                .Should().Be(newValue, "新しい値に変わっているはず");

            queuePropertyChanged.Dequeue()
                    .Should().Be(nameof(ValueHolder<string>.Value), "Valueプロパティの変更通知があったはず");
        }

        [Fact]
        public void Test_FileElement()
        {
            string targetFileName = "coopy -copy.txt";
            string targetFilePath = @"D:\FileRenamerDiff_Test\" + targetFileName;
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [targetFilePath] = new MockFileData(targetFilePath)
            });

            var fileElem = new FileElementModel(fileSystem, targetFilePath);
            var queuePropertyChanged = new Queue<string?>();
            fileElem.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

            fileElem.OutputFileName
                    .Should().Be(targetFileName, "まだ元のファイル名のまま");

            fileElem.IsReplaced
                .Should().BeFalse("まだリネーム変更されていないはず");

            fileElem.State
                .Should().Be(RenameState.None, "まだリネーム保存していない");

            queuePropertyChanged
                .Should().BeEmpty("まだ通知は来ていないはず");

            //ファイル名の一部をXXXに変更する置換パターンを作成
            var regex = new Regex(" -copy", RegexOptions.Compiled);
            var rpRegex = new ReplaceRegex(regex, "XXX");

            //リネームプレビュー実行
            fileElem.Replace(new[] { rpRegex }, false);

            const string expectedRenamedFileName = "coopyXXX.txt";

            fileElem.OutputFileName
                .Should().Be(expectedRenamedFileName, "リネーム変更後のファイル名になったはず");

            fileElem.IsReplaced
                .Should().BeTrue("リネーム変更されたはず");

            queuePropertyChanged
                .Should().Contain(new[] { nameof(FileElementModel.OutputFileName), nameof(FileElementModel.OutputFilePath), nameof(FileElementModel.IsReplaced) });

            fileElem.State
                .Should().Be(RenameState.None, "リネーム変更はしたが、まだリネーム保存していない");

            fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
                .Select(p => Path.GetFileName(p))
                .Should().BeEquivalentTo(new[] { targetFileName }, "ファイルシステム上はまだ前の名前のはず");

            //リネーム保存実行
            fileElem.Rename();

            fileElem.State
                .Should().Be(RenameState.Renamed, "リネーム保存されたはず");

            fileElem.InputFileName
                .Should().Be(expectedRenamedFileName, "リネーム保存後のファイル名になったはず");

            fileSystem.Directory.GetFiles(Path.GetDirectoryName(targetFilePath))
                .Select(p => Path.GetFileName(p))
                .Should().BeEquivalentTo(new[] { expectedRenamedFileName }, "ファイルシステム上も名前が変わったはず");
        }
    }
}
