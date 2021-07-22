﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Abstractions.TestingHelpers;

using FileRenamerDiff.Models;

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// デザイナー用のModel生成機
    /// </summary>
    class DesignerModel
    {
        const string targetDirPath = @"D:\FileRenamerDiff_Test";

        /// <summary>
        /// デザイナー用です　コードからは呼べません
        /// </summary>
        [Obsolete("Designer only", true)]
        public static MainModel MainModelForDesigner { get; } = CreateMainModelForDesigner();

        private static MainModel CreateMainModelForDesigner()
        {
            MockFileSystem fileSystem = CreateMockFileSystem();

            var model = new MainModel(fileSystem);
            model.Initialize();
            model.Setting.SearchFilePaths = new[] { targetDirPath };
            model.LoadFileElementsCore(null);
            model.ReplaceCore();
            return model;
        }

        private static MockFileSystem CreateMockFileSystem()
        {
            const string SubDirName = "D_SubDir";
            string filePathA = Path.Combine(targetDirPath, "A.htm");
            string filePathB = Path.Combine(targetDirPath, "B.csv");
            string filePathB2 = Path.Combine(targetDirPath, "B(1).csv");
            string filePathCini = Path.Combine(targetDirPath, "C.ini");
            string filePathDSubDir = Path.Combine(targetDirPath, SubDirName);
            string filePathE = Path.Combine(targetDirPath, SubDirName, "E.txt");
            string filePathFHidden = Path.Combine(targetDirPath, SubDirName, "F_Hidden.txt");
            string filePathGSubSubDir = Path.Combine(targetDirPath, SubDirName, "G_SubSub");
            string filePathHSubHiddenDir = Path.Combine(targetDirPath, "H_SubHidden");

            return new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [filePathA] = new MockFileData("A"),
                [filePathB] = new MockFileData("B"),
                [filePathB2] = new MockFileData("B2"),
                [filePathCini] = new MockFileData("C"),
                [filePathDSubDir] = new MockDirectoryData(),
                [filePathE] = new MockFileData(new byte[65536]),
                [filePathFHidden] = new MockFileData("F") { Attributes = FileAttributes.Hidden },
                [filePathGSubSubDir] = new MockDirectoryData(),
                [filePathHSubHiddenDir] = new MockDirectoryData() { Attributes = FileAttributes.Directory | FileAttributes.Hidden },
            });
        }
    }
}