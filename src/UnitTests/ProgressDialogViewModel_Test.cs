using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using FileRenamerDiff.Models;
using FileRenamerDiff.ViewModels;

using FluentAssertions;

using Moq;

using Reactive.Bindings;

using Xunit;

namespace UnitTests;

public class ProgressDialogViewModel_Test
{
    [WpfFact]
    public async Task ProgressInfo_RecievedOnlyLast()
    {
        var mock = new Mock<IMainModel>();
        var syncScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
        mock.SetupGet(x => x.UIScheduler)
            .Returns(syncScheduler);

        var subjectProgress = new Subject<ProgressInfo>();

        mock
            .SetupGet(x => x.CurrentProgressInfo)
            .Returns(subjectProgress.ToReadOnlyReactivePropertySlim());

        var vm = new ProgressDialogViewModel(mock.Object);

        var t = Task.Run(() =>
        {
            var pinfos = Enumerable.Range(0, 3)
                .Select(x => new ProgressInfo(x, $"progress-{x:00}"));

            foreach (var x in pinfos)
                subjectProgress.OnNext(x);
        });

        await vm.CurrentProgressInfo.WaitUntilValueChangedAsync();
        vm.CurrentProgressInfo.Value!.Count
            .Should().Be(2);
        vm.CurrentProgressInfo.Value!.Message
            .Should().Contain("progress-02");
        vm.CurrentProgressInfo.Value!.Message
            .Should().NotContainAny("00", "01");
    }

    [WpfFact]
    public async Task ProgressDialogViewModel_Cancel()
    {
        var mock = new Mock<IMainModel>();

        mock.SetupGet(x => x.UIScheduler)
            .Returns(new SynchronizationContextScheduler(SynchronizationContext.Current!));

        var subjectProgress = new Subject<ProgressInfo>();
        mock
            .SetupGet(x => x.CurrentProgressInfo)
            .Returns(subjectProgress.ToReadOnlyReactivePropertySlim());

        var cancelToken = new CancellationTokenSource();
        mock
            .SetupGet(x => x.CancelWork)
            .Returns(cancelToken);

        var vm = new ProgressDialogViewModel(mock.Object);

        cancelToken.IsCancellationRequested
            .Should().BeFalse();

        await vm.CancelCommand.ExecuteAsync();
        cancelToken.IsCancellationRequested
            .Should().BeTrue();

        await vm.CancelCommand.ExecuteAsync();
        cancelToken.IsCancellationRequested
            .Should().BeTrue();
    }
}
