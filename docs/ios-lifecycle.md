# iOS lifecycle cleanup

- Use `DisposableContentPage` with `CompositeDisposable` to collect page-level subscriptions and clear them in `OnDisappearing`.
- Store observer tokens and subscriptions as fields and dispose them at the matching lifecycle event.
- In debug builds, wrap subscriptions with `DisposeTracker.Track` and release them via `DisposeTracker.Dispose` to catch leaks early.
