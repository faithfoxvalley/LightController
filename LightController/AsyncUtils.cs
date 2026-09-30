using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LightController;

internal static class AsyncUtils
{
    public static void Run(Func<Task> asyncFunc)
    {
        Task.Run(() => asyncFunc()).GetAwaiter().GetResult();
    }

    public static T Run<T>(Func<Task<T>> asyncFunc)
    {
        return Task.Run(() => asyncFunc()).GetAwaiter().GetResult();
    }

    public static R Run<R, T1>(Func<T1, Task<R>> asyncFunc, T1 param1)
    {
        return Task.Run(() => asyncFunc(param1)).GetAwaiter().GetResult();
    }
}
