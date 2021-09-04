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
    /// <summary>
    /// テスト用拡張クラス
    /// </summary>
    public static class TestExt
    {
        /// <summary>
        /// スケジューラーを指定したミリ秒すすめる
        /// </summary>
        public static void AdvanceBy(this HistoricalScheduler scheduler, double millisec) =>
          scheduler.AdvanceBy(TimeSpan.FromMilliseconds(millisec));

        /// <summary>
        /// Idleになるまで待機する
        /// </summary>
        public static Task WaitIdle(this MainWindowViewModel vm) =>
            vm.IsIdle.Value
                ? Task.CompletedTask
                : vm.IsIdle.Where(x => x).FirstAsync().ToTask();

        /// <summary>
        /// UIIdleになるまで待機する
        /// </summary>
        public static Task WaitIdleUI(this MainModel m) =>
            m.IsIdleUI.Value
                ? Task.CompletedTask
                : m.IsIdleUI.Where(x => x).FirstAsync().ToTask();

        /// <summary>
        /// 指定した値が来るまで待機する
        /// </summary>
        public static Task WaitBe<T>(this IObservable<T> source, T expectValue) =>
                    source.Where(x => x?.Equals(expectValue) == true).FirstAsync().ToTask();

        /// <summary>
        /// 指定した値が来ることをテストする
        /// </summary>
        public static Task WaitShouldBe<T>(this IObservable<T> source, Func<T, bool> exprectedPredicate, double timeoutMilisec, string because)
        {
            Func<Task> func = () => source.WaitBe(exprectedPredicate).Timeout(timeoutMilisec);
            return func.Should().NotThrowAsync(because);
        }

        /// <summary>
        /// 指定した条件を満たす値が来るまで待機する
        /// </summary>
        public static Task WaitBe<T>(this IObservable<T> source, Func<T, bool> exprectedPredicate) =>
                    source.Where(x => exprectedPredicate(x)).FirstAsync().ToTask();

        /// <summary>
        /// 指定した条件を満たす値が来ることをテストする
        /// </summary>
        public static Task WaitShouldBe<T>(this IObservable<T> source, T expectValue, double timeoutMilisec, string because)
        {
            Func<Task> func = () => source.WaitBe(expectValue).Timeout(timeoutMilisec);
            return func.Should().NotThrowAsync(because);
        }

        /// <summary>
        /// IObservableを購読して貯めるリストを作成
        /// </summary>
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IObservable<T> sourceObservable)
        {
            var logList = new List<T>();

            sourceObservable
                .Subscribe(x =>
                    logList.Add(x));

            return logList;
        }
    }
}
