﻿using System.Diagnostics;

namespace Common;

public static class Repeat
{
    /// <summary>
    ///     Executes the specified <see cref="action" /> <see cref="count" /> times over in a loop.
    /// </summary>
    [DebuggerStepThrough]
    public static void Times(Action action, int count)
    {
        Times(action, 0, count);
    }

    /// <summary>
    ///     Executes the specified <see cref="action" /> <see cref="count" /> times over in a loop.
    /// </summary>
    [DebuggerStepThrough]
    public static void Times(Action<int> action, int count)
    {
        Times(action, 0, count);
    }

    /// <summary>
    ///     Executes the specified <see cref="action" /> <see cref="count" /> times over in a loop.
    /// </summary>
    [DebuggerStepThrough]
    public static async Task TimesAsync(Func<Task> action, int count)
    {
        await TimesAsync(action, 0, count);
    }

    /// <summary>
    ///     Executes the specified <see cref="action" /> <see cref="count" /> times over in a loop.
    /// </summary>
    [DebuggerStepThrough]
    public static async Task TimesAsync(Func<int, Task> action, int count)
    {
        await TimesAsync(action, 0, count);
    }

    [DebuggerStepThrough]
    private static void Times(Action action, int from, int to)
    {
        var counter = Enumerable.Range(from, to).ToList();
        counter.ForEach(_ => { action(); });
    }

    [DebuggerStepThrough]
    private static async Task TimesAsync(Func<Task> action, int from, int to)
    {
        var counter = Enumerable.Range(from, to).ToList();
        foreach (var _ in counter)
        {
            await action();
        }
    }

    [DebuggerStepThrough]
    private static void Times(Action<int> action, int from, int to)
    {
        var counter = Enumerable.Range(from, to).ToList();
        counter.ForEach(index => { action(index + 1); });
    }

    [DebuggerStepThrough]
    private static async Task TimesAsync(Func<int, Task> action, int from, int to)
    {
        var counter = Enumerable.Range(from, to).ToList();
        foreach (var count in counter)
        {
            await action(count);
        }
    }
}