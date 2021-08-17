using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

using DiffPlex.DiffBuilder.Model;

using FileRenamerDiff.Models;

using FluentAssertions;

using Reactive.Bindings;

using Xunit;

namespace UnitTests
{
    public class Test_AppExtension : IClassFixture<LogFixture>
    {
        [Fact]
        public void Test_ConcatenateString_string1() =>
            new[] { "a", "b", "c" }
                .ConcatenateString("WwW")
                .Should().Be("aWwWbWwWc");
        [Fact]
        public void Test_ConcatenateString_string2() =>
            new[] { "a", "b", "c" }
                .ConcatenateString("_")
                .Should().Be("a_b_c");

        [Fact]
        public void Test_Test_ConcatenateString_char() =>
            new[] { "a", "b", "c" }
                .ConcatenateString('_')
                .Should().Be("a_b_c");

        //別でテスト
        //[Fact]
        //public void Test_ToRawText() =>

        [Fact]
        public void Test_WithFreeze()
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
        public void Test_ToCode() =>
            Colors.Purple.ToCode()
                .Should().Be("#800080");

        [Fact]
        public void Test_ToColorOrDefault()
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
        public void Test_CodeToColorOrNull()
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
        public void Test_IEnumerable_Do()
        {
            string log = "log->";

            var arr = new[] { "a", "b", "c" }
                  .Do(x => log += x + "_")
                  .ToArray();

            log
                .Should().Be("log->a_b_c_");
        }

        [Fact]
        public void Test_IEnumerable_WithIndex()
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
        public void Test_IEnumerable_WhereNotNull() =>
            new string?[] { "a", null, "c" }
                .WhereNotNull()
                .ToArray()
                .Should().BeEquivalentTo("a", "c");

        [Fact]
        public void Test_IObservable_WhereNotNull()
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
        public void Test_CreateRegexOrNull()
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
        public void Test_IsValidRegexPattern(string pattern, bool asExpression, bool expectedIsValid) =>
            AppExtension.IsValidRegexPattern(pattern, asExpression)
                .Should().Be(expectedIsValid);

        [Fact]
        public async Task Test_TimeOut_Cause()
        {
            Func<Task> actionNotTimeOut = () => Task.Delay(1).Timeout(3000d);

            await actionNotTimeOut
                    .Should().NotThrowAsync();

            Func<Task> actionTimeOut = () => Task.Delay(3000).Timeout(100d);

            await actionTimeOut
                    .Should().ThrowAsync<TimeoutException>();
        }
    }
}
