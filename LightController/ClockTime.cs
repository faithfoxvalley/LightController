using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController
{
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
}
