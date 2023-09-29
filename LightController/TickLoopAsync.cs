using System;
using System.Threading.Tasks;

namespace LightController
{
    public class TickLoopAsync : TickLoop
    {
        public TickLoopAsync(double fps, Func<Task> onTick) : base(fps, () => RunOnThreadPool(onTick))
        {
        }

        private static void RunOnThreadPool(Func<Task> asyncFunction)
        {
            Task.Run(() => asyncFunction()).GetAwaiter().GetResult();
        }
    }
}
