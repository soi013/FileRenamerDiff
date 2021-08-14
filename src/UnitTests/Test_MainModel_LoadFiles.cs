﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using FileRenamerDiff.Models;

using FluentAssertions;

using Reactive.Bindings;

using Xunit;

namespace UnitTests
{
    public class Test_MainModel_LoadFiles
    {
        private const string targetDirPath = @"D:\FileRenamerDiff_Test";
        private const string SubDirName = "D_SubDir";
        private static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
        private static readonly string filePathB = Path.Combine(targetDirPath, "B.csv");
        private static readonly string filePathCini = Path.Combine(targetDirPath, "C.ini");
        private static readonly string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
        private static readonly string filePathE = Path.Combine(targetDirPath, SubDirName, "E.txt");
        private static readonly string filePathFHidden = Path.Combine(targetDirPath, SubDirName, "F_Hidden.txt");
        private static readonly string filePathGSubSubDir = Path.Combine(targetDirPath, SubDirName, "G_SubSub");
        private static readonly string filePathHSubHiddenDir = Path.Combine(targetDirPath, "H_SubHidden");

        private static MockFileSystem CreateMockFileSystem()
        {
            return new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [filePathA] = new MockFileData("A"),
                [filePathB] = new MockFileData("B"),
                [filePathCini] = new MockFileData("C"),
                [filePathDSubDir] = new MockDirectoryData(),
                [filePathE] = new MockFileData("E"),
                [filePathFHidden] = new MockFileData("F") { Attributes = FileAttributes.Hidden },
                [filePathGSubSubDir] = new MockDirectoryData(),
                [filePathHSubHiddenDir] = new MockDirectoryData() { Attributes = FileAttributes.Directory | FileAttributes.Hidden },
            });
        }

        private static MainModel CreateDefaultSettingModel(MockFileSystem? fileSystem = null)
        {
            fileSystem ??= CreateMockFileSystem();

            var model = new MainModel(fileSystem);
            model.Initialize();
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            return model;
        }

        [Fact]
        public async Task Test_LoadFileDefaultSetting_IgnoreIniHidden()
        {
            MainModel model = CreateDefaultSettingModel();

            await model.LoadFileElements();

            model.FileElementModels
                .Select(f => f.InputFilePath)
                .Should().BeEquivalentTo(
                    new[] { filePathA, filePathB, filePathDSubDir, filePathE, filePathGSubSubDir },
                    "iniファイルを除いた、トップ階層のファイルのみが列挙されるはず");
        }

        [Fact]
        public async Task Test_LoadFile_All()
        {
            MainModel model = CreateDefaultSettingModel();

            model.Setting.IgnoreExtensions.Clear();
            model.Setting.IsFileRenameTarget = true;
            model.Setting.IsDirectoryRenameTarget = true;
            model.Setting.IsHiddenRenameTarget = true;
            model.Setting.IsSearchSubDirectories = true;

            await model.LoadFileElements();

            model.FileElementModels
                .Select(f => f.InputFilePath)
                .Should().BeEquivalentTo(
                    new[] { filePathA, filePathB, filePathCini, filePathDSubDir, filePathE, filePathFHidden, filePathGSubSubDir, filePathHSubHiddenDir },
                    "すべてのファイル・フォルダが列挙されるはず");
        }

        [Fact]
        public async Task Test_LoadFile_OnlyDir()
        {
            MainModel model = CreateDefaultSettingModel();

            model.Setting.IgnoreExtensions.Clear();
            model.Setting.IsFileRenameTarget = false;
            model.Setting.IsDirectoryRenameTarget = true;
            model.Setting.IsHiddenRenameTarget = true;
            model.Setting.IsSearchSubDirectories = true;

            await model.LoadFileElements();

            model.FileElementModels
                .Select(f => f.InputFilePath)
                .Should().BeEquivalentTo(
                    new[] { filePathDSubDir, filePathGSubSubDir, filePathHSubHiddenDir },
                    "すべてのフォルダが列挙されるはず");
        }

        [Fact]
        public async Task Test_LoadFile_OnlyFile()
        {
            MainModel model = CreateDefaultSettingModel();

            model.Setting.IgnoreExtensions.Clear();
            model.Setting.IsFileRenameTarget = true;
            model.Setting.IsDirectoryRenameTarget = false;
            model.Setting.IsHiddenRenameTarget = true;
            model.Setting.IsSearchSubDirectories = true;

            await model.LoadFileElements();

            model.FileElementModels
                .Select(f => f.InputFilePath)
                .Should().BeEquivalentTo(
                    new[] { filePathA, filePathB, filePathCini, filePathE, filePathFHidden, },
                    "すべてのファイルが列挙されるはず");
        }

        [Fact]
        public async Task Test_LoadFile_OnlyTopIgnoreHidden()
        {
            MainModel model = CreateDefaultSettingModel();

            model.Setting.IgnoreExtensions.Clear();
            model.Setting.IsFileRenameTarget = true;
            model.Setting.IsDirectoryRenameTarget = true;
            model.Setting.IsHiddenRenameTarget = false;
            model.Setting.IsSearchSubDirectories = false;

            await model.LoadFileElements();

            model.FileElementModels
                .Select(f => f.InputFilePath)
                .Should().BeEquivalentTo(
                    new[] { filePathA, filePathB, filePathCini, filePathDSubDir },
                    "トップ階層の隠しファイル以外のファイル・フォルダが列挙されるはず");
        }

        [Fact]
        public async Task Test_LoadFile_Frequentory()
        {
            MainModel model = CreateDefaultSettingModel();

            for (int i = 0; i < 10; i++)
            {
                await model.LoadFileElements();
                model.FileElementModels.Count
                    .Should().Be(5);

                System.Diagnostics.Debug.WriteLine($"delay i:{i}");
                await Task.Delay(500);
            }
        }

        [Fact]
        public async Task Test_LoadFile_Nothing()
        {
            MainModel model = CreateDefaultSettingModel();

            model.Setting.IgnoreExtensions.Add(new("txt"));
            model.Setting.IgnoreExtensions.Add(new("csv"));

            model.Setting.IsDirectoryRenameTarget = false;
            model.Setting.IsSearchSubDirectories = false;

            Task<AppMessage> taskMessage = model.MessageEvent.FirstAsync().ToTask();

            await model.LoadFileElements();

            model.FileElementModels
                .Should().BeEmpty("ファイルがなにもないはず");

            (await taskMessage).MessageHead
                .Should().Be("NOT FOUND");
        }

        [Fact]
        public async Task Test_LoadFile_MannyFiles()
        {
            var files = Enumerable.Range(0, 1000)
                .Select(i => $"ManyFile-{i:0000}.txt")
                .ToDictionary(
                    x => Path.Combine(targetDirPath, x),
                    x => new MockFileData(x));

            MainModel model = CreateDefaultSettingModel(new MockFileSystem(files));

            var progressInfos = model.CurrentProgessInfo
                .Skip(1)
                .ToReadOnlyReactiveCollection();

            progressInfos
                .Should().BeEmpty("まだメッセージがないはず");

            await model.LoadFileElements();

            model.FileElementModels
                .Select(f => f.InputFilePath)
                    .Should().HaveCount(1000, "規定数ファイルが読み込まれたはず");

            progressInfos.Select(x => x.Message).Where(x => x.Contains("ManyFile"))
                .Should().HaveCountGreaterThan(2, "ファイル名を含んだメッセージがいくつか来たはず");
        }
    }
}
