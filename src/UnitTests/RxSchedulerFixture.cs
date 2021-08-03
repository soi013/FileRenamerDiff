using System;
using System.Linq;
using Reactive.Bindings;
using System.Reactive.Concurrency;

namespace UnitTests
{
    public class RxSchedulerFixture
    {
        public RxSchedulerFixture()
        {
            ReactivePropertyScheduler.SetDefault(TaskPoolScheduler.Default);
        }
    }
}