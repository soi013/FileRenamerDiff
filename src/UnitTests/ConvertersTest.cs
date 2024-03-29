﻿using System.Globalization;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using Anotar.Serilog;

using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Views;

namespace UnitTests;

public class ConvertersTest : IClassFixture<LogFixture>
{
    [WpfFact]
    public void LogEventLevelToBrushConverter()
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
    public void LogEventLevelToPackIconKindConverter()
    {
        LogEventLevelToPackIconKindConverter converter = new();

        Enum.GetValues<AppMessageLevel>()
            .Select(x => converter.Convert(x, 0, CultureInfo.InvariantCulture))
            .Should().OnlyHaveUniqueItems("違うアイコンのはず");

        converter.ConvertBack(0, 0, CultureInfo.InvariantCulture)
            .Should().Be(default);
    }

    [WpfFact]
    public void CultureDisplayConverter()
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
        Color falseColor = AppExtension.ToColorOrDefault("Yellow");
        Color trueColor = AppExtension.ToColorOrDefault("SkyBlue");
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

    private static readonly Color UnchangeC = AppExtension.ToColorOrDefault(DiffPaneModelToFlowDocumentConverter.UnchangeColorCode);
    private static readonly Color DeletedC = AppExtension.ToColorOrDefault(DiffPaneModelToFlowDocumentConverter.DeletedColorCode);
    private static readonly Color InsertedC = AppExtension.ToColorOrDefault(DiffPaneModelToFlowDocumentConverter.InsertedColorCode);

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
            SideBySideDiffModel diffModel = AppExtension.CreateDiff(oldFileName, newFileName);

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

        SideBySideDiffModel diffModel = AppExtension.CreateDiff(string.Empty, string.Empty);

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

        converter.Convert(-1L, 0, CultureInfo.InvariantCulture)
            .Should().Contain("--");

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

    [WpfFact]
    public void FileCategoryToStringConverter()
    {
        FileCategoryToStringConverter converter = new();

        converter.Convert(FileCategories.HiddenFile, 0, CultureInfo.InvariantCulture)
            .Should().Contain("Hidden");

        Enum.GetValues<FileCategories>()
            .Select(x => converter.Convert(x, 0, CultureInfo.InvariantCulture))
            .Should().OnlyHaveUniqueItems("違うアイコンのはず");

        converter.ConvertBack("hide", 0, CultureInfo.InvariantCulture)
            .Should().Be(default);
    }

    [WpfFact]
    public void FileCategoryToPackIconKindConverter()
    {
        FileCategoryToPackIconKindConverter converter = new();

        Enum.GetValues<FileCategories>()
            .Select(x => (int)converter.Convert(x, 0, CultureInfo.InvariantCulture))
            .Should().OnlyHaveUniqueItems();

        converter.ConvertBack(0, typeof(Enum), 0, CultureInfo.InvariantCulture)
            .Should().Be(default(FileCategories));
    }
}
