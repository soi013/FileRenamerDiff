using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;

using FluentAssertions;

using Reactive.Bindings;

namespace UnitTests
{
    public static class TestExt
    {
        public static void AdvanceBy(this HistoricalScheduler scheduler, double millisec) =>
          scheduler.AdvanceBy(TimeSpan.FromMilliseconds(millisec));

        public static Task WaitIdle(this MainWindowViewModel vm) =>
            vm.IsIdle.Where(x => x).FirstAsync().ToTask();

        public static Task WaitIdle(this MainModel m) =>
            m.IsIdle.Where(x => x).FirstAsync().ToTask();

        public static Task WaitBe<T>(this IObservable<T> source, T expectValue) =>
                    source.Where(x => x?.Equals(expectValue) == true).FirstAsync().ToTask();

        public static Task WaitShouldBe<T>(this IObservable<T> source, T expectValue, double timeoutMilisec, string because)
        {
            Func<Task> func = () => source.WaitBe(expectValue).Timeout(timeoutMilisec);
            return func.Should().NotThrowAsync(because);
        }
    }
}
