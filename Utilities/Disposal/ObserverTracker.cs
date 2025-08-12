#if DEBUG
using System;
using System.Runtime.CompilerServices;

namespace FlockForge.Utilities.Disposal
{
    static class ObserverTracker
    {
        static readonly ConditionalWeakTable<IDisposable, string> Notes = new();

        public static T Mark<T>(T d, [CallerFilePath] string f = "", [CallerLineNumber] int l = 0)
            where T : class, IDisposable
        {
            Notes.Add(d, $"{System.IO.Path.GetFileName(f)}:{l}");
            return d;
        }
    }
}
#endif
