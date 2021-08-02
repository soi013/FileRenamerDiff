using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Xunit;
using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;

using FileRenamerDiff.Models;

namespace UnitTests
{
    public class Test_MainModel_Setting
    {
        private static readonly string defaultSettingFilePath = SettingAppModel.DefaultFilePath;
        private static readonly string otherSettingFilePath = @"D:\FileRenamerDiff_Setting\Setting.json";

        [Fact]
        public void Test_NoSettingSaveSetting()
        {
            var noSettingFileSystem = new MockFileSystem();

            var model = new MainModel(noSettingFileSystem);

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
        public void Test_LoadSetting()
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

            var model = new MainModel(mockFileSystem);

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
        public void Test_SettingReset()
        {
            var noSettingFileSystem = new MockFileSystem();

            var model = new MainModel(noSettingFileSystem);
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
    }
}
