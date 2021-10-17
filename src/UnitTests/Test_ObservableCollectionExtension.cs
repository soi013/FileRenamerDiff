using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive;
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
using Reactive.Bindings.Extensions;

using Xunit;

namespace UnitTests
{
    public class ObservableCollectionExtension_Test : IClassFixture<LogFixture>
    {
        [Fact]
        public void ToObservableCollection() =>
            new[] { "a", "b", "c" }
                .Select(x => x.ToUpperInvariant())
                .ToObservableCollection()
                .Should().BeEquivalentTo(new[] { "A", "B", "C" });

        [Fact]
        public void AddRange()
        {
            var source = new[] { "a", "b", "c" }.ToObservableCollection();
            source.AddRange(new[] { "x", "y", "z" });

            source
                .Should().BeEquivalentTo(new[] { "a", "b", "c", "x", "y", "z" });
        }

        [Fact]
        public void RemoveAll()
        {
            var source = new[] { 1, 2, 3, 4, 5 }.ToObservableCollection();
            source
                .RemoveAll(x => x % 2 == 0);

            source
                .Should().BeEquivalentTo(new[] { 1, 3, 5 });
        }

        [Fact]
        public void ObserveX()
        {
            var source = new[] { "a", "b", "c" }.ToObservableCollection();
            var countLog = source.ObserveCount()
                .ToReadOnlyList();
            var anyLog = source.ObserveIsAny()
                .ToReadOnlyList();
            var emptyLog = source.ObserveIsEmpty()
                .ToReadOnlyList();

            source.Add("x");
            source.Clear();
            source.Add("A");

            countLog
                .Should().BeEquivalentTo(new[] { 3, 4, 0, 1 });

            emptyLog
                 .Should().BeEquivalentTo(new[] { false, true, false });

            anyLog
               .Should().BeEquivalentTo(new[] { true, false, true });
        }

        [Fact]
        public void ToObservableCollctionSynced()
        {
            ObservableCollection<int> source = new[] { 1, 2, 3 }.ToObservableCollection();
            ObservableCollection<double> syncTarget = source.ToObservableCollctionSynced(
                x => x * 100d,
                d => (int)d / 100);

            source.Add(99);
            syncTarget
                .Should().BeEquivalentTo(new[] { 100d, 200d, 300d, 9900d });

            syncTarget.Add(-123);
            source
                .Should().BeEquivalentTo(new[] { 1, 2, 3, 99, -1 });

            source.Remove(2);
            syncTarget
                .Should().BeEquivalentTo(new[] { 100d, 300d, 9900d, -123d });

            syncTarget.RemoveAt(1);
            source
                .Should().BeEquivalentTo(new[] { 1, 99, -1 });

            source[0] = 5;
            syncTarget
                .Should().BeEquivalentTo(new[] { 500d, 9900d, -123d });

            syncTarget.Move(1, 2);

            source
                .Should().BeEquivalentTo(new[] { 5, -1, 99 });

            syncTarget.Clear();

            source
                .Should().BeEmpty();
            syncTarget
                .Should().BeEmpty();
        }
    }
}
