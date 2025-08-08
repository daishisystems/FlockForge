using System;

namespace FlockForge.Utilities.Disposal
{
    public sealed class ActionDisposable : IDisposable
    {
        private Action? _dispose;
        public ActionDisposable(Action dispose) => _dispose = dispose;
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
}