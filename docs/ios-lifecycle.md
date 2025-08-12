# iOS lifecycle notes

- Subscribe to observers and Rx streams in `OnAppearing` and dispose in `OnDisappearing`.
- Pages inheriting from `DisposableContentPage` get a `CompositeDisposable` that is cleared on disappearance; debug builds can wrap tokens with `DisposeTracker`.
- If a page uses `WebView`, call `DisposableContentPage.StopWebView` in `OnDisappearing` to stop loading and clear its source.
