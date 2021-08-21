using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using FileRenamerDiff.Models;

using FluentAssertions;

using Xunit;

namespace UnitTests
{
    public class Test_MainModel_Setting
    {
        private static readonly string defaultSettingFilePath = SettingAppModel.DefaultFilePath;
        private static readonly string otherSettingFilePath = @"D:\FileRenamerDiff_Setting\Setting.json";

        [Fact]
        public void Test_SaveSetting()
        {
            var noSettingFileSystem = new MockFileSystem();

            var model = new MainModel(noSettingFileSystem, Scheduler.Immediate);

            model.Initialize();

            const string newIgnoreExt = "someignoreext";
            model.Setting.IgnoreExtensions.Add(new(newIgnoreExt));

            model.SaveSettingFile(otherSettingFilePath);

            noSettingFileSystem.AllFiles
                .Should().BeEquivalentTo(new[] { otherSettingFilePath }, because: "設定ファイルが保存されているはず");

            noSettingFileSystem.File.ReadAllText(otherSettingFilePath)
                .Should().Contain(newIgnoreExt, because: "設定ファイルの中身に新しい設定値が保存されているはず");
        }

        [Fact]
        public async Task Test_SaveSetting_Cancel()
        {
            var noSettingFileSystem = new MockFileSystem();

            var model = new MainModel(noSettingFileSystem, Scheduler.Immediate);

            model.Initialize();

            const string newIgnoreExt = "someignoreext";
            model.Setting.IgnoreExtensions.Add(new(newIgnoreExt));

            Task<AppMessage> taskMessage = model.MessageEvent.FirstAsync().ToTask();
            model.SaveSettingFile("   ");

            noSettingFileSystem.AllFiles
                .Should().BeEmpty("設定ファイルはが保存されていないはず");

            await Task.Delay(100);

            taskMessage.IsCompleted
                .Should().BeFalse("空白のファイルパスはファイルダイアログのキャンセルなので、何もメッセージを出さない");
        }

        [Fact]
        public async Task Test_SaveSetting_FailInvalidFilePath()
        {
            var noSettingFileSystem = new MockFileSystem();
            noSettingFileSystem.AddFile(otherSettingFilePath, new MockFileData("other") { AllowedFileShare = FileShare.Read });

            var model = new MainModel(noSettingFileSystem, Scheduler.Immediate);

            model.Initialize();

            const string newIgnoreExt = "someignoreext";
            model.Setting.IgnoreExtensions.Add(new(newIgnoreExt));

            Task<AppMessage> taskMessage = model.MessageEvent.FirstAsync().ToTask();
            model.SaveSettingFile(otherSettingFilePath);

            noSettingFileSystem.AllFiles
                .Should().BeEquivalentTo(new[] { otherSettingFilePath }, because: "設定ファイル自体はあるはず");

            noSettingFileSystem.File.ReadAllText(otherSettingFilePath)
                .Should().NotContain(newIgnoreExt, because: "設定ファイルの中身に新しい設定値が保存されていないはず");

            AppMessage appMessage = await taskMessage.Timeout(3000d);
            appMessage.MessageLevel
                .Should().Be(AppMessageLevel.Error);
            appMessage.MessageBody
                .Should().Contain(otherSettingFilePath);
        }

        [Fact]
        public void Test_LoadSetting_Success()
        {
            const string firstIgnoreExt = "firstignoreext";
            const string otherIgnoreExt = "otherignoreext";

            string settingContent = @"{""IgnoreExtensions"":[{""Value"":""" + firstIgnoreExt + @"""}]}";
            string settingContentOther = @"{""IgnoreExtensions"":[{""Value"":""" + otherIgnoreExt + @"""}]}";

            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [defaultSettingFilePath] = new MockFileData(settingContent),
                [otherSettingFilePath] = new MockFileData(settingContentOther),
            });

            var model = new MainModel(mockFileSystem, Scheduler.Immediate);

            var queuePropertyChanged = new Queue<string?>();
            model.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

            model.Initialize();

            model.Setting.IgnoreExtensions.Select(x => x.Value)
                .Should().Contain(firstIgnoreExt, because: "起動時にファイルから設定値が読み込まれたはず");

            model.LoadSettingFile(otherSettingFilePath);

            model.Setting.IgnoreExtensions.Select(x => x.Value)
                .Should().NotContain(firstIgnoreExt, because: "別の設定ファイルを読ませたら、元の設定値は消えたはず");

            model.Setting.IgnoreExtensions.Select(x => x.Value)
                .Should().Contain(otherIgnoreExt, because: "別の設定ファイルを読ませたら、ファイルから設定値が読み込まれたはず");

            queuePropertyChanged
                .Should().BeEquivalentTo(new[] { nameof(MainModel.Setting) }, because: "設定変更通知が来たはず");
        }

        [Fact]
        public void Test_LoadSetting_Fail()
        {
            const string firstIgnoreExt = "firstignoreext";

            string settingContent = @"{""IgnoreExtensions"":[{""Value"":""" + firstIgnoreExt + @"""}]}";

            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [defaultSettingFilePath] = new MockFileData(settingContent),
                [otherSettingFilePath] = new MockFileData("aaa"),
            });

            var model = new MainModel(mockFileSystem, Scheduler.Immediate);
            model.Initialize();

            model.Setting.IgnoreExtensions.Select(x => x.Value)
                .Should().Contain(firstIgnoreExt, because: "起動時にファイルから設定値が読み込まれたはず");

            model.LoadSettingFile("  ");
            model.LoadSettingFile(otherSettingFilePath + "__.json");
            model.LoadSettingFile(otherSettingFilePath);

            model.Setting.IgnoreExtensions.Select(x => x.Value)
                .Should().Contain(firstIgnoreExt, because: "無効な設定ファイルなら、元の設定値のままのはず");
        }

        [Fact]
        public void Test_SettingReset()
        {
            var noSettingFileSystem = new MockFileSystem();

            var model = new MainModel(noSettingFileSystem, Scheduler.Immediate);
            var queuePropertyChanged = new Queue<string?>();
            model.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

            model.Initialize();

            const string firstIgnoreExt = "someignoreext";
            model.Setting.IgnoreExtensions.Add(new(firstIgnoreExt));

            model.ResetSetting();

            model.Setting.IgnoreExtensions.Select(x => x.Value)
                .Should().NotContain(firstIgnoreExt, because: "別の設定ファイルを読ませたら、元の設定値は消えたはず");

            queuePropertyChanged
               .Should().BeEquivalentTo(new[] { nameof(MainModel.Setting) }, because: "設定変更通知が来たはず");
        }

        [Theory]
        [InlineData("de", "how_to_use.de.md")]
        [InlineData("ja", "how_to_use.ja.md")]
        [InlineData("ja-jp", "how_to_use.ja.md")]
        [InlineData("ru", "how_to_use.ru.md")]
        [InlineData("zh", "how_to_use.zh.md")]
        [InlineData("xx", "how_to_use.md")]
        [InlineData("en", "how_to_use.md")]
        public void Test_GetHelpPath_DefaultSetting(string langCode, string expectedHelpFileName)
        {
            var noSettingFileSystem = new MockFileSystem();

            var model = new MainModel(noSettingFileSystem, Scheduler.Immediate);
            model.Initialize();

            Thread.CurrentThread.CurrentCulture = new CultureInfo(langCode);

            model.GetHelpPath()
                .Should().Contain(expectedHelpFileName);
        }

        [Theory]
        [InlineData("de", "how_to_use.de.md")]
        [InlineData("ja", "how_to_use.ja.md")]
        [InlineData("ru", "how_to_use.ru.md")]
        [InlineData("zh", "how_to_use.zh.md")]
        [InlineData("xx", "how_to_use.md")]
        [InlineData("en", "how_to_use.md")]
        [InlineData("ja-jp", "how_to_use.md")]
        public void Test_GetHelpPath_Setting(string langCode, string expectedHelpFileName)
        {
            var noSettingFileSystem = new MockFileSystem();

            var model = new MainModel(noSettingFileSystem, Scheduler.Immediate);
            model.Initialize();

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            model.Setting.AppLanguageCode = langCode;

            model.GetHelpPath()
                .Should().Contain(expectedHelpFileName);
        }
    }
}
