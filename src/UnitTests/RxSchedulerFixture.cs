using System;
using System.Linq;
using Reactive.Bindings;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Threading;

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