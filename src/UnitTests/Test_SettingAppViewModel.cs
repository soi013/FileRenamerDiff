using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;

using FluentAssertions;

using Reactive.Bindings;

using Xunit;

namespace UnitTests
{
    public class SettingAppViewModel_Test : IClassFixture<LogFixture>
    {
        private const string targetDirPath = @"D:\FileRenamerDiff_Test";
        private const string targetDirPathSub = @"D:\FileRenamerDiff_TestSub";

        [WpfFact]
        public async Task Commands_CanExecute()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();
            await model.WaitIdleUI().Timeout(3000d);
            await Task.Delay(100);

            //ステージ1 初期状態
            var canExecuteUsuallyCommands = new ICommand[]
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

            foreach ((ICommand c, int i) in canExecuteUsuallyCommands.WithIndex())
            {
                c.CanExecute(null)
                    .Should().BeTrue($"すべて実行可能はなず (indexCommand:{i})");
            }
        }

        [Fact]
        public void SearchFilePathConcate()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
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

        [WpfFact]
        public void CommonDeletePattern()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();

            const string commonDeletePatternTarget = @".*";
            CommonPatternViewModel commonDeletePattern = settingVM.CommonDeletePatternVMs
                    .Where(x => x.TargetPattern == commonDeletePatternTarget)
                    .First();

            //ステージ1 追加前
            commonDeletePattern.TargetPattern
                .Should().Be(commonDeletePatternTarget);
            commonDeletePattern.ReplaceText
                .Should().BeEmpty();
            commonDeletePattern.AsExpression
                .Should().BeTrue();
            commonDeletePattern.SampleDiff.OldText.ToRawText()
                .Should().NotBeEmpty();
            commonDeletePattern.SampleDiff.NewText.ToRawText()
                .Should().BeEmpty();

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

        [WpfFact]
        public void CommonReplacePattern()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();

            //英数小文字を大文字にするパターン(ex. low -> LOW)を取得
            const string commonReplacePatternTarget = "[a-z]";
            CommonPatternViewModel commonReplacePattern = settingVM.CommonReplacePatternVMs
                    .Where(x => x.TargetPattern == commonReplacePatternTarget)
                    .First();

            //ステージ1 追加前
            commonReplacePattern.TargetPattern
                .Should().Be(commonReplacePatternTarget);
            commonReplacePattern.ReplaceText
                .Should().Contain("\\u");
            commonReplacePattern.AsExpression
                .Should().BeTrue();
            commonReplacePattern.SampleDiff.OldText.ToRawText()
                .Should().Contain("low");
            commonReplacePattern.SampleDiff.NewText.ToRawText()
                .Should().Contain("LOW");

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

        [WpfFact]
        public void OtherProperties()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();

            var defaultSetting = new SettingAppModel();

            //ステージ1 変更前
            settingVM.IsSearchSubDirectories.Value
                .Should().Be(defaultSetting.IsSearchSubDirectories, "デフォルト設定と同じはず");

            settingVM.IsDirectoryRenameTarget.Value
                .Should().Be(defaultSetting.IsDirectoryRenameTarget, "デフォルト設定と同じはず");

            settingVM.IsFileRenameTarget.Value
                .Should().Be(defaultSetting.IsFileRenameTarget, "デフォルト設定と同じはず");

            settingVM.IsHiddenRenameTarget.Value
                .Should().Be(defaultSetting.IsHiddenRenameTarget, "デフォルト設定と同じはず");

            settingVM.IsRenameExt.Value
                .Should().Be(defaultSetting.IsRenameExt, "デフォルト設定と同じはず");

            settingVM.IsAppDarkTheme.Value
                .Should().Be(defaultSetting.IsAppDarkTheme, "デフォルト設定と同じはず");

            settingVM.IsCreateRenameLog.Value
                .Should().Be(defaultSetting.IsCreateRenameLog, "デフォルト設定と同じはず");

            //ステージ2 変更後
            settingVM.IsSearchSubDirectories.Value ^= true;
            settingVM.IsDirectoryRenameTarget.Value ^= true;
            settingVM.IsFileRenameTarget.Value ^= true;
            settingVM.IsHiddenRenameTarget.Value ^= true;
            settingVM.IsRenameExt.Value ^= true;
            settingVM.IsAppDarkTheme.Value ^= true;
            settingVM.IsCreateRenameLog.Value ^= true;

            settingVM.IsSearchSubDirectories.Value
                .Should().Be(!defaultSetting.IsSearchSubDirectories, "デフォルト設定と違うはず");

            settingVM.IsDirectoryRenameTarget.Value
                .Should().Be(!defaultSetting.IsDirectoryRenameTarget, "デフォルト設定と違うはず");

            settingVM.IsFileRenameTarget.Value
                .Should().Be(!defaultSetting.IsFileRenameTarget, "デフォルト設定と違うはず");

            settingVM.IsHiddenRenameTarget.Value
                .Should().Be(!defaultSetting.IsHiddenRenameTarget, "デフォルト設定と違うはず");

            settingVM.IsRenameExt.Value
                .Should().Be(!defaultSetting.IsRenameExt, "デフォルト設定と違うはず");

            settingVM.IsAppDarkTheme.Value
                .Should().Be(!defaultSetting.IsAppDarkTheme, "デフォルト設定と違うはず");

            settingVM.IsCreateRenameLog.Value
                .Should().Be(!defaultSetting.IsCreateRenameLog, "デフォルト設定と違うはず");
        }

        [WpfFact]
        public async Task Add_Clear_DeleteTextsCommand()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();
            await model.WaitIdleUI().Timeout(3000d);
            await Task.Delay(100);

            int defaultCount = model.Setting.DeleteTexts.Count;
            //ステージ1 追加後
            settingVM.AddDeleteTextsCommand.Execute();

            model.Setting.DeleteTexts
                .Should().HaveCount(defaultCount + 1, "1つ増えているはず");
            model.Setting.DeleteTexts.Last().ReplaceText
                .Should().BeEmpty("追加された内容は空白のはず");
            model.Setting.DeleteTexts.Last().TargetPattern
                .Should().BeEmpty("追加された内容は空白のはず");

            //ステージ2 クリア後
            await settingVM.ClearDeleteTextsCommand.ExecuteAsync();

            model.Setting.DeleteTexts
                .Should().BeEmpty("空になったはず");
        }

        [WpfFact]
        public async Task Add_Clear_IgnoreExtensionsCommand()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();
            await model.WaitIdleUI().Timeout(3000d);
            await Task.Delay(100);

            int defaultCount = model.Setting.IgnoreExtensions.Count;

            //ステージ1 追加後
            settingVM.AddIgnoreExtensionsCommand.Execute();

            model.Setting.IgnoreExtensions
                .Should().HaveCount(defaultCount + 1, "1つ増えているはず");
            model.Setting.IgnoreExtensions.Last().Value
                .Should().BeEmpty("追加された内容は空白のはず");

            //ステージ2 クリア後
            await settingVM.ClearIgnoreExtensionsCommand.ExecuteAsync();

            model.Setting.IgnoreExtensions
                .Should().BeEmpty("空になったはず");
        }

        [WpfFact]
        public async Task Add_Clear_ReplaceTextsCommand()
        {
            var model = new MainModel(new MockFileSystem(), Scheduler.Immediate);
            var settingVM = new SettingAppViewModel(model);
            model.Initialize();
            await model.WaitIdleUI().Timeout(3000d);
            await Task.Delay(100);

            int defaultCount = model.Setting.ReplaceTexts.Count;

            //ステージ1 追加後
            settingVM.AddReplaceTextsCommand.Execute();

            model.Setting.ReplaceTexts
                .Should().HaveCount(defaultCount + 1, "1つ増えているはず");
            model.Setting.ReplaceTexts.Last().ReplaceText
                .Should().BeEmpty("追加された内容は空白のはず");
            model.Setting.ReplaceTexts.Last().TargetPattern
                .Should().BeEmpty("追加された内容は空白のはず");

            //ステージ2 クリア後
            await settingVM.ClearReplaceTextsCommand.ExecuteAsync();

            model.Setting.ReplaceTexts
                .Should().BeEmpty("空になったはず");
        }
    }
}
