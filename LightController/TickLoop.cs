using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightController
{
    public class TickLoop
    {
        private const int ErrorTimeout = 5000;

        private Timer timer;
        private Func<Task> onTick;
        private readonly int msPerTick;
        private readonly int ticksPerSecond;
        private int actualTicksPerSecond;
        private long usagePerSecond;
        private long maxUsagePerSecond;

        public TickLoop(double fps, Func<Task> onTick)
        {
            this.onTick = onTick;
            msPerTick = (int)Math.Round(1000d / fps);
            ticksPerSecond = (int)Math.Round(1000d / msPerTick);

            // https://stackoverflow.com/a/12797382
            timer = new Timer(Tick, null, msPerTick, Timeout.Infinite);
        }

        private async void Tick(object state)
        {

            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                await onTick();

                Interlocked.Increment(ref actualTicksPerSecond);
                Interlocked.Add(ref usagePerSecond, sw.ElapsedMilliseconds);
                sw.Stop();
                timer.Change(Math.Max(0, msPerTick - sw.ElapsedMilliseconds), Timeout.Infinite);
            }
            catch (Exception ex)
            {
                LogFile.Error(ex, "An error occurred while updating!");
                timer.Change(ErrorTimeout, Timeout.Infinite);
            }
        }

        // To be called once per second on UI thread
        public void AppendPerformanceInfo(StringBuilder sb)
        {
            int ticks = Interlocked.Exchange(ref actualTicksPerSecond, 0);
            sb.Append(ticks).Append(" / ").Append(ticksPerSecond).Append(" fps").AppendLine();

            long usage = Interlocked.Exchange(ref usagePerSecond, 0);
            double maxUsage = ticksPerSecond * msPerTick;
            sb.AppendFormat("{0:P}", usage / maxUsage).AppendLine();

            long maxDmxUsage = Interlocked.Read(ref maxUsagePerSecond);
            if (maxDmxUsage < usage)
            {
                maxDmxUsage = usage;
                Interlocked.Exchange(ref maxUsagePerSecond, maxDmxUsage);
            }
            sb.Append("Max: ").AppendFormat("{0:P}", maxDmxUsage / maxUsage).AppendLine();
        }

        public void Disable()
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}
