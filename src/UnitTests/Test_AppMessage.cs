using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using FileRenamerDiff.Models;

using FluentAssertions;

using Xunit;

namespace UnitTests
{
    public class Test_AppMessage
    {
        [Fact]
        public void AppMessageHeadMerge()
        {
            var messages = new AppMessage[]
            {
                new (AppMessageLevel.Info, "HEADTEXT", "A1"),
                new (AppMessageLevel.Info, "HEADTEXT", "A2"),
                new (AppMessageLevel.Info, "HEADTEXT", "A3"),
                new (AppMessageLevel.Info, "OTHER_HEAD", "B1"),
                new (AppMessageLevel.Info, "OTHER_HEAD", "B2"),
                new (AppMessageLevel.Info, "HEADTEXT", "C1"),
                new (AppMessageLevel.Info, "HEADTEXT", "C2"),
                new (AppMessageLevel.Alert, "SINGLE", "D1"),
                new (AppMessageLevel.Error, "MIX_LEVEL", "E1"),
                new (AppMessageLevel.Alert, "MIX_LEVEL", "E2"),
                new (AppMessageLevel.Info, "MIX_LEVEL", "E3"),
            };

            var sumMessages = new Queue<AppMessage>(AppMessageExt.SumSameHead(messages));

            var sum1 = sumMessages.Dequeue();
            sum1.MessageLevel
                .Should().Be(AppMessageLevel.Info);

            sum1.MessageHead
                .Should().Be("HEADTEXT");

            sum1.MessageBody
                .Should().Be($"A1{Environment.NewLine}A2{Environment.NewLine}A3");

            var sum2 = sumMessages.Dequeue();
            sum2.MessageLevel
                .Should().Be(AppMessageLevel.Info);

            sum2.MessageHead
                .Should().Be("OTHER_HEAD");

            sum2.MessageBody
                .Should().Be($"B1{Environment.NewLine}B2");

            var sum3 = sumMessages.Dequeue();
            sum3.MessageLevel
                .Should().Be(AppMessageLevel.Info);

            sum3.MessageHead
                .Should().Be("HEADTEXT");

            sum3.MessageBody
                .Should().Be($"C1{Environment.NewLine}C2");

            var sum4 = sumMessages.Dequeue();
            sum4.MessageLevel
                .Should().Be(AppMessageLevel.Alert);

            sum4.MessageHead
                .Should().Be("SINGLE");

            sum4.MessageBody
                .Should().Be("D1");

            var sum5 = sumMessages.Dequeue();
            sum5.MessageLevel
                .Should().Be(AppMessageLevel.Error);

            sum5.MessageHead
                .Should().Be("MIX_LEVEL");

            sum5.MessageBody
                .Should().Be($"E1{Environment.NewLine}E2{Environment.NewLine}E3");
        }
    }
}
