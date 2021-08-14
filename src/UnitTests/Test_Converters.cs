﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Anotar.Serilog;

using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;
using FileRenamerDiff.Views;

using FluentAssertions;
using FluentAssertions.Extensions;
using FluentAssertions.Primitives;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Schedulers;

using Xunit;

namespace UnitTests
{
    public class Test_Converters : IClassFixture<LogFixture>
    {
        [WpfFact]
        public void Test_LogEventLevelToBrushConverter()
        {
            LogEventLevelToBrushConverter converter = new();
            ((SolidColorBrush)converter.Convert(AppMessageLevel.Info, 0, CultureInfo.InvariantCulture))
                .Color
                .Should().Be(Colors.White);

            ((SolidColorBrush)converter.Convert(AppMessageLevel.Alert, 0, CultureInfo.InvariantCulture))
                .Color
                .Should().Be(Colors.Orange);

            ((SolidColorBrush)converter.Convert(AppMessageLevel.Error, 0, CultureInfo.InvariantCulture))
                 .Color
                 .Should().Be(Colors.Red);

            converter.ConvertBack(Colors.Orange.ToSolidColorBrush(), 0, CultureInfo.InvariantCulture)
                .Should().Be(AppMessageLevel.Info);
        }

        [WpfFact]
        public void Test_CultureDisplayConverter()
        {
            CultureDisplayConverter converter = new();
            converter.Convert(CultureInfo.InvariantCulture, 0, CultureInfo.InvariantCulture)
                .Should().Contain("Auto");

            var englishCulture = CultureInfo.GetCultureInfo("en");

            Thread.CurrentThread.CurrentUICulture = englishCulture;

            converter.Convert(CultureInfo.GetCultureInfo("en"), 0, englishCulture)
                .Should().ContainAll("en", "English");
            converter.Convert(CultureInfo.GetCultureInfo("ru"), 0, englishCulture)
                .Should().ContainAll("ru", "русский");
            converter.Convert(CultureInfo.GetCultureInfo("ja"), 0, englishCulture)
                .Should().ContainAll("ja", "日本語");
            converter.Convert(CultureInfo.GetCultureInfo("zh"), 0, englishCulture)
                .Should().ContainAll("zh", "中文");
            converter.Convert(CultureInfo.GetCultureInfo("de"), 0, englishCulture)
                .Should().ContainAll("de", "Deutsch");
            converter.Convert(CultureInfo.InvariantCulture, 0, englishCulture)
                .Should().Contain("Auto");

            converter.ConvertBack("ja", 0, englishCulture).Name
                .Should().Be("ja");
        }

        [WpfFact]
        public void BoolToBrushConverter()
        {
            Color falseColor = AppExtention.ToColorOrDefault("Yellow");
            Color trueColor = AppExtention.ToColorOrDefault("SkyBlue");
            BoolToBrushConverter converter = new()
            {
                FalseBrush = falseColor.ToSolidColorBrush(),
                TrueBrush = trueColor.ToSolidColorBrush(),
            };

            ((SolidColorBrush)converter.Convert(true, 0, CultureInfo.InvariantCulture)).Color
                .Should().Be(trueColor);
            ((SolidColorBrush)converter.Convert(false, 0, CultureInfo.InvariantCulture)).Color
                 .Should().Be(falseColor);

            converter.ConvertBack(trueColor.ToSolidColorBrush(), 0, CultureInfo.InvariantCulture)
                .Should().BeFalse();
        }

        private static readonly Color UnchangeC = AppExtention.ToColorOrDefault(DiffPaneModelToFlowDocumentConverter.UnchangeColorCode);
        private static readonly Color DeletedC = AppExtention.ToColorOrDefault(DiffPaneModelToFlowDocumentConverter.DeletedColorCode);
        private static readonly Color InsertedC = AppExtention.ToColorOrDefault(DiffPaneModelToFlowDocumentConverter.InsertedColorCode);

        [WpfFact]
        public void DiffPaneMToFlowDocConverter()
        {
            LogTo.Information("Start");

            DiffPaneModelToFlowDocumentConverter converter = new();

            var testCases = new (string oldFileName, string newFileName, Color[] oldColors, Color[] newColors)[]
            {
                ("abc.txt", "abc.txt", new[]{ UnchangeC }, new[]{ UnchangeC }),
                ("abc.txt", "def.txt", new[]{ DeletedC, UnchangeC, UnchangeC }, new[]{ InsertedC, UnchangeC, UnchangeC }),
                ("abc.txt", "azc.txt", new[]{ DeletedC, UnchangeC, UnchangeC }, new[]{ InsertedC, UnchangeC, UnchangeC }),
                ("fix tgt_fix", "fix REP_fix", new[]{ UnchangeC, UnchangeC, DeletedC, UnchangeC, UnchangeC }, new[]{ UnchangeC, UnchangeC, InsertedC, UnchangeC, UnchangeC }),
                ("Del.ete.txt", "Del..txt", new[]{ UnchangeC, DeletedC, DeletedC, DeletedC, UnchangeC }, new[]{ UnchangeC,  InsertedC,  UnchangeC }),
            };

            foreach (var (oldFileName, newFileName, oldColors, newColors) in testCases)
            {
                SideBySideDiffModel diffModel = AppExtention.CreateDiff(oldFileName, newFileName);

                //inlineDataの配列は1つしか受け取れない制限のため、同じ組み合わせでoldとnewを別テストメソッドで行う
                DiffPaneMSide(converter, diffModel.OldText, oldColors);
                DiffPaneMSide(converter, diffModel.NewText, newColors);
            }
        }

        private static void DiffPaneMSide(DiffPaneModelToFlowDocumentConverter converter, DiffPaneModel diffPaneModel, IEnumerable<Color> expectedColors)
        {
            var flowDoc = (FlowDocument)converter.Convert(diffPaneModel, typeof(DiffPaneModel), 0, CultureInfo.InvariantCulture);

            IEnumerable<Color> actualBrushes = ((Paragraph)flowDoc.Blocks.FirstBlock).Inlines
                                .Select(x => (SolidColorBrush)x.Background)
                                .Select(x => x.Color);
            actualBrushes
                .Should().BeEquivalentTo(expectedColors, "背景色が差分に合わせた色で区切られているはず");
        }

        [WpfFact]
        public void DiffPaneMToFlowDocConverter_DoNothing()
        {
            LogTo.Information("Start");

            DiffPaneModelToFlowDocumentConverter converter = new();
            converter.Convert(new object(), typeof(DiffPaneModel), 0, CultureInfo.InvariantCulture)
                .Should().Be(Binding.DoNothing);

            SideBySideDiffModel diffModel = AppExtention.CreateDiff(string.Empty, string.Empty);

            converter.Convert(diffModel.OldText, typeof(DiffPaneModel), 0, CultureInfo.InvariantCulture)
                .Should().Be(Binding.DoNothing);

            converter.ConvertBack(new FlowDocument(new Paragraph()), typeof(FlowDocument), 0, CultureInfo.InvariantCulture)
                .Should().Be(Binding.DoNothing);
        }

        [WpfFact]
        public void ReadableByteTextConverter()
        {
            ReadableByteTextConverter converter = new();

            converter.Convert(123L, 0, CultureInfo.InvariantCulture)
                .Should().Be("123 B");
            converter.Convert(2049L, 0, CultureInfo.InvariantCulture)
                .Should().Be("2 KB");
            converter.Convert(1024L * 1024 * 3, 0, CultureInfo.InvariantCulture)
                .Should().Be("3 MB");
            converter.Convert(1024L * 1024 * 1024 * 4, 0, CultureInfo.InvariantCulture)
                .Should().Be("4 GB");
            converter.Convert(1024L * 1024 * 1024 * 1024 * 5, 0, CultureInfo.InvariantCulture)
                .Should().Be("5 TB");

            converter.ConvertBack("123B", 0, CultureInfo.InvariantCulture)
                .Should().Be(0);
        }

        [WpfFact]
        public void ReadableByteTextConverter_AsGenericConverter()
        {
            ReadableByteTextConverter converter = new();

            converter.Convert(123L, typeof(string), 0, CultureInfo.InvariantCulture)
                .Should().Be("123 B");

            converter.ConvertBack("123B", typeof(string), 0, CultureInfo.InvariantCulture)
                .Should().Be(0);
        }
    }
}