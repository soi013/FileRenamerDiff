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
    public class Test_Model
    {
        [Fact]
        public async Task Test_LoadFiles()
        {
            string targetDirPath = @"D:\FileRenamerDiff_Test\";
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                [targetDirPath + "A.txt"] = new MockFileData("A"),
                [targetDirPath + "B.txt"] = new MockFileData("B"),
                [targetDirPath + "C.txt"] = new MockFileData("C"),
            });

            var model = new Model(fileSystem);
            model.Initialize();
            model.Setting.SearchFilePaths = new[] { targetDirPath };

            await model.LoadFileElements();

            model.FileElementModels
                .Select(f => f.InputFileName)
                .Should().BeEquivalentTo("A.txt", "B.txt", "C.txt");
        }
    }
}
