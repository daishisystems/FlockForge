#if DEBUG
using System;
using System.Runtime.CompilerServices;

namespace FlockForge.Utilities.Disposal
{
    static class DisposeTracker
    {
        static readonly ConditionalWeakTable<IDisposable, string> Notes = new();

        public static T Track<T>(T d, string owner, string note) where T : class, IDisposable
        {
            Notes.Add(d, $"{owner}:{note}");
            return d;
        }

        public static void Dispose(ref IDisposable? d)
        {
            d?.Dispose();
            d = null;
        }
    }
}
#endif
