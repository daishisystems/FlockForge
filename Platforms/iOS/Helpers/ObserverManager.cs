using System;
using System.Collections.Generic;
using Foundation;

namespace FlockForge.Platforms.iOS.Helpers
{
    public class ObserverManager : IDisposable
    {
        private readonly List<NSObject> _observers = new();
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        public void AddObserver(NSObject observer)
        {
            if (observer != null)
                _observers.Add(observer);
        }

        public void AddSubscription(IDisposable subscription)
        {
            if (subscription != null)
                _subscriptions.Add(subscription);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var observer in _observers)
                        observer?.Dispose();
                    
                    foreach (var subscription in _subscriptions)
                        subscription?.Dispose();
                    
                    _observers.Clear();
                    _subscriptions.Clear();
                }
                _disposed = true;
            }
        }
    }
}