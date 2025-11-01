using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AM.Mind.Util;

public static class ProcessExitFlusher
{
    private static IDisposable? _disposable;

    public static void Register(IDisposable d)
    {
        _disposable = d;
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        Console.CancelKeyPress += OnCancel;
    }

    private static void OnExit(object? s, EventArgs e) => _disposable?.Dispose();
    private static void OnCancel(object? s, ConsoleCancelEventArgs e) => _disposable?.Dispose();
}
