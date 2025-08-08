using System;
using System.Reactive.Disposables;
using Microsoft.Maui.Controls;

namespace FlockForge.Views.Base
{
    public abstract class BaseContentPage : ContentPage, IDisposable
    {
        public readonly CompositeDisposable Disposables = new();
        private bool _disposed;

        protected override void OnAppearing()
        {
            base.OnAppearing();
            OnAppearingInternal();
        }

        protected virtual void OnAppearingInternal() { }

        protected override void OnDisappearing()
        {
            OnDisappearingInternal();
            Disposables.Clear(); // dispose page-scoped subscriptions/observers
            base.OnDisappearing();
        }

        protected virtual void OnDisappearingInternal() { }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}