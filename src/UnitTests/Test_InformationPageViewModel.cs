using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;

using FluentAssertions;

using Xunit;

namespace UnitTests
{
    public class Test_InformationPageViewModel
    {
        [Fact]
        public void Test_InformationPageViewModel_Create()
        {
            InformationPageViewModel.AppInfoText
                .Should().ContainAll("File Renamer Diff", "soi013");
        }
    }
}
