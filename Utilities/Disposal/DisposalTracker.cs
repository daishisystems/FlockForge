#if DEBUG
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FlockForge.Utilities.Disposal
{
    public static class DisposalTracker
    {
        private static readonly ConcurrentDictionary<string,int> Active = new();
        public static void Track(string key, bool created)
        {
            if (created) { Active.AddOrUpdate(key, 1, (_,v)=>v+1); Debug.WriteLine($"[DISPOSAL]+ {key} => {Active[key]}"); }
            else if (Active.TryGetValue(key, out var v)) { Active[key] = v>0 ? v-1 : 0; Debug.WriteLine($"[DISPOSAL]- {key} => {Active[key]}"); }
        }
    }
}
#endif