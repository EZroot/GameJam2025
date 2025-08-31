using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Lightweight async event bus for Unity 6.
/// </summary>
public static class EventHub
{
    /// <summary>Concurrent so you can safely sub/unsub from worker threads.</summary>
    private static readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public static void Subscribe<T>(Func<T, Task> handler) where T : struct
    {
        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());

        lock (list)      // keep list edits thread-safe
        {
            list.Add(handler);
        }

        Debug.Log($"<color=orange>Subscribed</color> → {typeof(T).Name}");
    }

    public static void Unsubscribe<T>(Func<T, Task> handler) where T : struct
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;

        lock (list)
        {
            list.Remove(handler);
            if (list.Count == 0) _handlers.TryRemove(typeof(T), out _);
        }

        Debug.Log($"<color=orange>Unsubscribed</color> ← {typeof(T).Name}");
    }

    /// <summary>
    /// Fire the event and await all handlers in sequence.
    /// </summary>
    public static async Task RaiseAsync<T>(T eventArg) where T : struct
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;

        Delegate[] snapshot;
        lock (list)          // avoid mutation during enumeration
        {
            snapshot = list.ToArray();
        }

        foreach (var d in snapshot.Cast<Func<T, Task>>())
        {
            try
            {
                // .ConfigureAwait(false) keeps you off the main thread.
                // If your handler touches Unity APIs, push it back to the main thread first.
                await d(eventArg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in async handler for {typeof(T).Name}: {ex}");
            }
        }
    }
}
