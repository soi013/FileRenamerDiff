using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Input;

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
    public class Test_SettingAppViewModel : IClassFixture<RxSchedulerFixture>
    {
        private const string targetDirPath = @"D:\FileRenamerDiff_Test";
        private const string targetDirPathSub = @"D:\FileRenamerDiff_TestSub";
        private const string fileNameA = "A.txt";
        private const string fileNameB = "B.csv";
        private static readonly string filePathA = Path.Combine(targetDirPath, fileNameA);
        private static readonly string filePathB = Path.Combine(targetDirPath, fileNameB);


        [Fact]
        public void Test_CommandCanExecute()
        {
            var model = new MainModel(new MockFileSystem());
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();

            //ステージ1 初期状態
            var canExecuteUsuallyCommand = new ICommand[]
            {
                settingVM.AddDeleteTextsCommand,
                settingVM.AddIgnoreExtensionsCommand,
                settingVM.AddReplaceTextsCommand,
                settingVM.ClearDeleteTextsCommand,
                settingVM.ClearIgnoreExtensionsCommand,
                settingVM.ClearReplaceTextsCommand,
                settingVM.LoadSettingFileDialogCommand,
                settingVM.ResetSettingCommand,
                settingVM.SaveSettingFileDialogCommand,
                settingVM.ShowExpressionReferenceCommand,
            };

            canExecuteUsuallyCommand
                .ForEach(c =>
                    c.CanExecute(null)
                    .Should().BeTrue("すべて実行可能はなず"));
        }

        [Fact]
        public void Test_SearchFilePathConcate()
        {
            var model = new MainModel(new MockFileSystem());
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();

            IReadOnlyList<string> newPaths = new[] { targetDirPath, targetDirPathSub };
            settingVM.SearchFilePaths.Value = newPaths;
            settingVM.ConcatedSearchFilePaths.Value
                .Should().Contain(targetDirPath, "連結ファイルパスに元のファイルパスが含まれているはず")
                .And.Contain(targetDirPathSub, "連結ファイルパスに元のファイルパスが含まれているはず");



            settingVM.ConcatedSearchFilePaths.Value = $"{targetDirPath}|{targetDirPathSub}";
            settingVM.SearchFilePaths.Value
                .Should().Contain(targetDirPath, "連結ファイルパスに元のファイルパスが含まれているはず")
                .And.Contain(targetDirPathSub, "連結ファイルパスに元のファイルパスが含まれているはず");
        }


        [Fact]
        public void Test_CommonDeletePattern()
        {
            var model = new MainModel(new MockFileSystem());
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();

            const string commonDeletePatternTarget = @".*";
            CommonPatternViewModel commonDeletePattern = settingVM.CommonDeletePatternVMs
                    .Where(x => x.TargetPattern == commonDeletePatternTarget)
                    .First();

            //ステージ1 追加前
            settingVM.DeleteTexts
                .Should().NotContain(x => x.TargetPattern.Value == commonDeletePatternTarget, "まだ含まれていないはず");

            //ステージ2 追加後
            commonDeletePattern.AddSettingCommand.Execute();
            settingVM.DeleteTexts
                .Should().Contain(x => x.TargetPattern.Value == commonDeletePatternTarget, "含まれているはず");

            //ステージ3 追加後編集
            ReplacePatternViewModel addedPattern = settingVM.DeleteTexts.Where(x => x.TargetPattern.Value == commonDeletePatternTarget).First();
            const string changedTarget = "XXX";
            addedPattern.TargetPattern.Value = changedTarget;

            settingVM.CommonDeletePatternVMs
                .Should().NotContain(x => x.TargetPattern == changedTarget, "追加先で編集しても、元のプロパティは変更されないはず")
                .And.Contain(x => x.TargetPattern == commonDeletePatternTarget, "追加先で編集しても、元のプロパティは変更されないはず");

            CommonPattern.DeletePatterns
                .Should().NotContain(x => x.TargetPattern == changedTarget, "追加先で編集しても、元のプロパティは変更されないはず")
                .And.Contain(x => x.TargetPattern == commonDeletePatternTarget, "追加先で編集しても、元のプロパティは変更されないはず");
        }

        [Fact]
        public void Test_CommonReplacePattern()
        {
            var model = new MainModel(new MockFileSystem());
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();

            const string commonReplacePatternTarget = "[a-z]";
            CommonPatternViewModel commonReplacePattern = settingVM.CommonReplacePatternVMs
                    .Where(x => x.TargetPattern == commonReplacePatternTarget)
                    .First();

            //ステージ1 追加前
            settingVM.ReplaceTexts
                .Should().NotContain(x => x.TargetPattern.Value == commonReplacePatternTarget, "まだ含まれていないはず");

            //ステージ2 追加後
            commonReplacePattern.AddSettingCommand.Execute();
            settingVM.ReplaceTexts
                .Should().Contain(x => x.TargetPattern.Value == commonReplacePatternTarget, "含まれているはず");

            //ステージ3 追加後編集
            ReplacePatternViewModel addedPattern = settingVM.ReplaceTexts.Where(x => x.TargetPattern.Value == commonReplacePatternTarget).First();
            const string changedTarget = "XXX";
            addedPattern.TargetPattern.Value = changedTarget;

            settingVM.CommonReplacePatternVMs
                .Should().NotContain(x => x.TargetPattern == changedTarget, "追加先で編集しても、元のプロパティは変更されないはず")
                .And.Contain(x => x.TargetPattern == commonReplacePatternTarget, "追加先で編集しても、元のプロパティは変更されないはず");

            CommonPattern.ReplacePatterns
                .Should().NotContain(x => x.TargetPattern == changedTarget, "追加先で編集しても、元のプロパティは変更されないはず")
                .And.Contain(x => x.TargetPattern == commonReplacePatternTarget, "追加先で編集しても、元のプロパティは変更されないはず");
        }
    }
}
