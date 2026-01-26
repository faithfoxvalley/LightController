using System;
using System.Diagnostics;

namespace LightController;

public static class ClockTime
{
    private static Stopwatch timer = new Stopwatch();
    private static DateTime start;

    public static DateTime UtcNow => start + timer.Elapsed;

    public static void Init()
    {
        start = DateTime.UtcNow;
        timer.Restart();
    }
}
