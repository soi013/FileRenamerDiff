using System.Threading;

using Reactive.Bindings;

namespace FileRenamerDiff.Models
{
    public interface IMainModel
    {
        public IReadOnlyReactiveProperty<ProgressInfo?> CurrentProgressInfo { get; }
        public CancellationTokenSource? CancelWork { get; }
    }
}
