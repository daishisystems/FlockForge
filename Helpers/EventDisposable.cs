using System;
using FlockForge.Core.Util;

namespace FlockForge.Helpers
{
    public static class EventDisposable
    {
        public static IDisposable Subscribe<TEventArgs>(
            EventHandler<TEventArgs>? source,
            EventHandler<TEventArgs> handler,
            Action<EventHandler<TEventArgs>> addHandler,
            Action<EventHandler<TEventArgs>> removeHandler)
        {
            addHandler(handler);
            return new ActionDisposable(() => removeHandler(handler));
        }
    }
}