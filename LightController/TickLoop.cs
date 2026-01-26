using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace LightController;

public class TickLoop : IDisposable
{
    private const int ErrorTimeout = 5000;

    private readonly Action onTick;
    private readonly double msPerTick;
    private readonly int ticksPerSecond;

    private int actualTicksPerSecond;
    private long usagePerSecond;
    private long maxUsagePerSecond;

    private CancellationTokenSource cts = new CancellationTokenSource();

    private Thread thread;

    public TickLoop (double fps, Action onTick)
    {
        this.onTick = onTick;

        msPerTick = TimeSpan.FromSeconds (1 / fps).TotalMilliseconds;
        ticksPerSecond = (int)Math.Round(fps);

        thread = new Thread(TickThread);
        thread.IsBackground = true;
        thread.Start();
    }

    public void Dispose()
    {
        cts.Cancel();
    }

    private void TickThread()
    {
        try
        {
            Stopwatch sw = new Stopwatch();
            while (!cts.IsCancellationRequested)
            {
                sw.Restart();

                try
                {
                    onTick();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while updating!");
                    Thread.Sleep(ErrorTimeout);
                }

                Interlocked.Increment(ref actualTicksPerSecond);
                Interlocked.Add(ref usagePerSecond, sw.ElapsedMilliseconds);

                double ms = msPerTick - sw.Elapsed.TotalMilliseconds;
                if (ms > 0)
                    Thread.Sleep((int)ms);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while updating, updates will not continue!");
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
}
