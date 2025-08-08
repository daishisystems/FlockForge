using System.Reactive.Disposables;
using Foundation;

namespace FlockForge.Platforms.iOS.Extensions;
public static class DisposableExtensions
{
    public static void Add(this CompositeDisposable cd, NSObject token)
        => cd.Add(new FlockForge.Core.Util.ActionDisposable(() => token.Dispose()));
}