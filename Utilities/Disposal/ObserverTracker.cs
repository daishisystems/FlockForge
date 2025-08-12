#if DEBUG
using System;
using System.Runtime.CompilerServices;
namespace FlockForge.Utilities.Disposal
{
    static class ObserverTracker
    {
        static readonly ConditionalWeakTable<IDisposable, string> Notes = new();

        public static T Mark<T>(T d, string site)
            where T : class, IDisposable
        {
            Notes.Add(d, site);
            return d;
        }
    }
}
#endif
