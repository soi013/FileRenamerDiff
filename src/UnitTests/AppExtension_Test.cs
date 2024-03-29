﻿using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace UnitTests;

public class AppExtension_Test : IClassFixture<LogFixture>
{
    [Fact]
    public void ConcatenateString_string1() =>
        new[] { "a", "b", "c" }
            .ConcatenateString("WwW")
            .Should().Be("aWwWbWwWc");
    [Fact]
    public void ConcatenateString_string2() =>
        new[] { "a", "b", "c" }
            .ConcatenateString("_")
            .Should().Be("a_b_c");

    [Fact]
    public void ConcatenateString_char() =>
        new[] { "a", "b", "c" }
            .ConcatenateString('_')
            .Should().Be("a_b_c");

    //別でテスト
    //[Fact]
    //public void ToRawText() =>

    [Fact]
    public void WithFreeze()
    {
        var brush = Colors.Red.ToSolidColorBrush();
        brush.IsFrozen
            .Should().BeFalse();
        brush.Color = Colors.Blue;

        brush = brush.WithFreeze();
        brush.IsFrozen
            .Should().BeTrue();
        brush.Color
            .Should().Be(Colors.Blue);

        var brushFreezed = Colors.Yellow.ToSolidColorBrush(isFreeze: true);
        brushFreezed.IsFrozen
            .Should().BeTrue();
    }

    [Fact]
    public void ToCode() =>
        Colors.Purple.ToCode()
            .Should().Be("#800080");

    [Fact]
    public void ToColorOrDefault()
    {
        AppExtension.ToColorOrDefault("Purple")
            .Should().Be(Colors.Purple);

        AppExtension.ToColorOrDefault("#800080")
            .Should().Be(Colors.Purple);

        AppExtension.ToColorOrDefault("nazonoiro")
            .Should().Be(default(Color));
        AppExtension.ToColorOrDefault("#XXYYZZ")
            .Should().Be(default(Color));
    }

    [Fact]
    public void CodeToColorOrNull()
    {
        AppExtension.CodeToColorOrNull("Purple")
            .Should().Be(Colors.Purple);

        AppExtension.CodeToColorOrNull("#800080")
            .Should().Be(Colors.Purple);

        AppExtension.CodeToColorOrNull("nazonoiro")
            .Should().BeNull();
        AppExtension.CodeToColorOrNull("#XXYYZZ")
            .Should().BeNull();
    }

    [Fact]
    public void IEnumerable_Do()
    {
        string log = "log->";

        var arr = new[] { "a", "b", "c" }
              .Do(x => log += x + "_")
              .ToArray();

        log
            .Should().Be("log->a_b_c_");
    }

    [Fact]
    public void IEnumerable_DoWithIndex()
    {
        string log = "log->";

        var arr = new[] { "a", "b", "c" }
              .Do((x, i) => log += $"[{i}-{x}]_")
              .ToArray();

        log
            .Should().Be("log->[0-a]_[1-b]_[2-c]_");
    }

    [Fact]
    public void IEnumerable_WithIndex()
    {
        string log = "log->";

        foreach (var (x, i) in new[] { "a", "b", "c" }.WithIndex())
        {
            log += $"{i:00}-{x}_";
        }

        log
            .Should().Be("log->00-a_01-b_02-c_");
    }

    [Fact]
    public void IEnumerable_WhereNotNull() =>
        new string?[] { "a", null, "c" }
            .WhereNotNull()
            .ToArray()
            .Should().BeEquivalentTo("a", "c");

    [Fact]
    public void IObservable_WhereNotNull()
    {
        string log = "log->";
        var subject = new Subject<string?>();

        subject
            .WhereNotNull()
            .Subscribe(x => log += x + "_");

        subject.OnNext("a");
        subject.OnNext(null);
        subject.OnNext("c");

        log
            .Should().Be("log->a_c_");
    }

    [Fact]
    public void CreateRegexOrNull()
    {
        AppExtension.CreateRegexOrNull("abc")
            .Should().NotBeNull();
        AppExtension.CreateRegexOrNull("\\w")
            .Should().NotBeNull();
        AppExtension.CreateRegexOrNull("\\l")
            .Should().BeNull();
    }

    [Theory]
    [InlineData("abc", false, true)]
    [InlineData("abc", true, true)]
    [InlineData(" ", false, true)]
    [InlineData(" ", true, true)]
    [InlineData("", false, false)]
    [InlineData("", true, false)]
    [InlineData(null, false, false)]
    [InlineData(null, true, false)]
    [InlineData("\\l", false, true)]
    [InlineData("\\l", true, false)]
    public void IsValidRegexPattern(string pattern, bool asExpression, bool expectedIsValid) =>
        AppExtension.IsValidRegexPattern(pattern, asExpression)
            .Should().Be(expectedIsValid);


    [Theory]
    [InlineData("abc", false, true)]
    [InlineData("abc", true, true)]
    [InlineData(" ", false, true)]
    [InlineData(" ", true, true)]
    [InlineData("", false, true)]
    [InlineData("", true, true)]
    [InlineData(null, false, false)]
    [InlineData(null, true, false)]
    [InlineData("$t<a>", true, false)]
    [InlineData("$t<a>", false, true)]
    public void IsValidReplacePattern(string pattern, bool asExpression, bool expectedIsValid) =>
        AppExtension.IsValidReplacePattern(pattern, asExpression)
            .Should().Be(expectedIsValid);

    [Fact]
    public async Task TimeOut_Cause()
    {
        Func<Task> actionNotTimeOut = () => Task.Delay(1).Timeout(3000d);

        await actionNotTimeOut
                .Should().NotThrowAsync();

        Func<Task> actionTimeOut = () => Task.Delay(3000).Timeout(100d);

        await actionTimeOut
                .Should().ThrowAsync<TimeoutException>();
    }

    enum TrafficLight
    {
        [EnumMember(Value = "Stop")]
        Red,
        Yello,
        [EnumMember(Value = "Go")]
        Blue,
    }

    [Fact]
    public void EnumExt()
    {
        TrafficLight.Red.GetAttribute<TrafficLight, EnumMemberAttribute>()?.Value
            .Should().Be("Stop");

        TrafficLight.Yello.GetAttribute<TrafficLight, EnumMemberAttribute>()?.Value
            .Should().BeNull();

        TrafficLight.Blue.GetAttribute<TrafficLight, EnumMemberAttribute>()?.Value
            .Should().Be("Go");
    }

    [Fact]
    public void StringExts()
    {
        string? sNull = null;
        string? sEmpt = string.Empty;
        string? sWhit = "  ";
        string? sText = "abc";

        sNull.IsNullOrEmpty().Should().BeTrue();
        sEmpt.IsNullOrEmpty().Should().BeTrue();
        sWhit.IsNullOrEmpty().Should().BeFalse();
        sText.IsNullOrEmpty().Should().BeFalse();

        sNull.IsNullOrWhiteSpace().Should().BeTrue();
        sEmpt.IsNullOrWhiteSpace().Should().BeTrue();
        sWhit.IsNullOrWhiteSpace().Should().BeTrue();
        sText.IsNullOrWhiteSpace().Should().BeFalse();

        sNull.HasText().Should().BeFalse();
        sEmpt.HasText().Should().BeFalse();
        sWhit.HasText().Should().BeFalse();
        sText.HasText().Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_Empty()
    {
        Array.Empty<int>().IsEmpty()
            .Should().BeTrue();

        Enumerable.Range(0, 0).IsEmpty()
            .Should().BeTrue();

        Enumerable.Range(0, 10).Where(x => x < 0).IsEmpty()
            .Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_NotEmpty()
    {
        new[] { 0, 1, 2 }.IsEmpty()
            .Should().BeFalse();

        Enumerable.Range(0, 10).IsEmpty()
            .Should().BeFalse();
    }

    [Fact]
    public void ToIntOrNull_Int()
    {
        "1".ToIntOrNull()
            .Should().Be(1);

        "-99".ToIntOrNull()
            .Should().Be(-99);

        "005".ToIntOrNull()
            .Should().Be(5);
    }

    [Fact]
    public void ToIntOrNull_Null()
    {
        (null as string)?.ToIntOrNull()
             .Should().BeNull();

        string.Empty.ToIntOrNull()
             .Should().BeNull();

        "abc".ToIntOrNull()
             .Should().BeNull();

        "a99".ToIntOrNull()
            .Should().BeNull();

        "0.01".ToIntOrNull()
            .Should().BeNull();
    }

    [Fact]
    public void ToDictionaryDirectKey_Noraml()
    {
        var dict = new[] { "a", "bb", "ccc" }
            .ToDictionaryDirectKey(x => x.Length);

        dict.Keys
            .Should().BeEquivalentTo(new[] { "a", "bb", "ccc" });

        dict.Values
            .Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ToDictionaryDirectKey_Empty()
    {
        var dict = Array.Empty<string>()
            .ToDictionaryDirectKey(x => x.Length);

        dict.Should().BeEmpty();
    }

    [Fact]
    public void CreateMockFileSystem_Noraml()
    {
        string parentDirPath = @"D:\pdir";

        var paths = new[] { "abc.txt", "def.csv", "subdir" }
            .Select(x => Path.Combine(parentDirPath, x));

        var fileSystem = AppExtension.CreateMockFileSystem(paths);

        fileSystem.Directory.EnumerateFileSystemEntries(parentDirPath, "*", SearchOption.AllDirectories)
            .Should().BeEquivalentTo(new[]
            {
                @$"{parentDirPath}\abc.txt",
                @$"{parentDirPath}\def.csv",
                @$"{parentDirPath}\subdir",
            });

        fileSystem.Directory.GetDirectories(parentDirPath)
            .Should().BeEmpty();
    }

    [Fact]
    public void CreateMockFileSystem_SubSubDir()
    {
        string parentDirPath = @"D:\pdir";

        string subDirName = "SUB";

        var paths = new[]
            {
                "abc.txt",
                Path.Combine(subDirName,"def.csv"),
                Path.Combine(subDirName,subDirName,"ghi.ini")
            }
            .Select(x => Path.Combine(parentDirPath, x));

        var fileSystem = AppExtension.CreateMockFileSystem(paths);

        fileSystem.Directory.EnumerateFileSystemEntries(parentDirPath, "*", SearchOption.AllDirectories)
            .Should().BeEquivalentTo(new[]
            {
                @$"{parentDirPath}\abc.txt",
                @$"{parentDirPath}\{subDirName}",
                @$"{parentDirPath}\{subDirName}\def.csv",
                @$"{parentDirPath}\{subDirName}\{subDirName}",
                @$"{parentDirPath}\{subDirName}\{subDirName}\ghi.ini",
            });

        fileSystem.Directory.GetDirectories(parentDirPath, "*", SearchOption.AllDirectories)
           .Should().BeEquivalentTo(new[]
                {
                    @$"{parentDirPath}\{subDirName}",
                    @$"{parentDirPath}\{subDirName}\{subDirName}",
                });
    }
}
