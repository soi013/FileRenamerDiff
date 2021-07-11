using System;
using Xunit;
using FileRenamerDiff.Models;
using System.Collections.Generic;
using FluentAssertions;
using System.Text.RegularExpressions;
using System.IO.Abstractions.TestingHelpers;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests
{
    public class Test_Model_LoadFiles
    {
        const string targetDirPath = @"D:\FileRenamerDiff_Test";
        const string SubDirName = "D_SubDir";
        static readonly string filePathA = Path.Combine(targetDirPath, "A.txt");
        static readonly string filePathB = Path.Combine(targetDirPath, "B.csv");
        static readonly string filePathCini = Path.Combine(targetDirPath, "C.ini");
        static readonly string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
        static readonly string filePathE = Path.Combine(targetDirPath, SubDirName, "E.txt");
        static readonly string filePathFHidden = Path.Combine(targetDirPath, SubDirName, "F_Hidden.txt");
        static readonly string filePathGSubSubDir = Path.Combine(targetDirPath, SubDirName, "G_SubSub");
        static readonly string filePathHSubHiddenDir = Path.Combine(targetDirPath, "H_SubHidden");

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
        private static Model CreateDefaultSettingModel()
        {
            MockFileSystem fileSystem = CreateMockFileSystem();

            var model = new Model(fileSystem);
            model.Initialize();
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            return model;
        }

        [Fact]
        public async Task Test_LoadFileDefaultSetting_OnlyTopOnlyFileIgnoreIniHidden()
        {
            Model model = CreateDefaultSettingModel();

            await model.LoadFileElements();

            model.FileElementModels
                .Select(f => f.InputFilePath)
                .Should().BeEquivalentTo(
                    new[] { filePathA, filePathB, },
                    "iniファイルを除いた、トップ階層のファイルのみが列挙されるはず");
        }

        [Fact]
        public async Task Test_LoadFile_All()
        {
            Model model = CreateDefaultSettingModel();

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
            Model model = CreateDefaultSettingModel();

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
            Model model = CreateDefaultSettingModel();

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
            Model model = CreateDefaultSettingModel();

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
    }
}
