using System;
using Xunit;
using FileRenamerDiff.Models;
using System.Collections.Generic;
using FluentAssertions;

namespace UnitTests
{
    public class UnitTest1
    {
        [Fact]
        public void Test_ValueHolder()
        {
            var queuePropertyChanged = new Queue<string?>();
            var holder = ValueHolderFactory.Create(string.Empty);

            holder.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

            holder.Value
                .Should().BeEmpty("初期値は空のはず");

            queuePropertyChanged
                .Should().BeEmpty("まだ通知は来ていないはず");

            const string newValue = "NEW_VALUE";
            holder.Value = newValue;

            holder.Value
                .Should().Be(newValue, "新しい値に変わっているはず");

            queuePropertyChanged.Dequeue()
                    .Should().Be(nameof(ValueHolder<string>.Value), "Valueプロパティの変更通知があったはず");
        }
    }
}
