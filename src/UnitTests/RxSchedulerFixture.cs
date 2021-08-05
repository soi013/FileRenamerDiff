using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;

using Reactive.Bindings;

namespace UnitTests
{
    public class RxSchedulerFixture
    {
        public RxSchedulerFixture()
        {
            ReactivePropertyScheduler.SetDefault(new SynchronizationContextScheduler(SynchronizationContext.Current!));
        }
    }
}
