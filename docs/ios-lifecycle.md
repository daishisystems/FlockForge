# iOS Lifecycle Patterns

- Use `DisposableContentPage` to maintain a `CompositeDisposable` that clears subscriptions in `OnDisappearing`.
- Create observers or Rx subscriptions in `OnAppearing` and dispose them in `OnDisappearing` using `DisposeTracker` for debug builds.
- Unregister event handlers and optional WebView sources when pages disappear to avoid iOS observer warnings.
