using System.Diagnostics;

internal class Program
{
    private static void Main(string[] args)
    {
        int total = 100;
        
        Stopwatch sw = new Stopwatch();
        for(int ms = 1; ms < 100; ms++)
        { 
            TimeSpan totalTime = TimeSpan.Zero;
            for (int i = 0; i < total; i++)
            {
                sw.Restart();
                Thread.Sleep(ms); // Request a 1ms sleep
                sw.Stop();
                TimeSpan time = sw.Elapsed;
                totalTime += time;
            }

            TimeSpan average = totalTime / total;
            Console.WriteLine($"{ms}: {average.TotalMilliseconds:0.00}");
        }
    }
}