using System.Reactive.Concurrency;

using Reactive.Bindings;

namespace FileRenamerDiff.Models;

public interface IMainModel
{
    public IReadOnlyReactiveProperty<ProgressInfo?> CurrentProgressInfo { get; }
    public CancellationTokenSource? CancelWork { get; }
    IScheduler UIScheduler { get; }
}
