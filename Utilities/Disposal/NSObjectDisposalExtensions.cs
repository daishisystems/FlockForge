#if IOS
using Foundation;
using FlockForge.Core.Util;

namespace FlockForge.Utilities.Disposal
{
    public static class NSObjectDisposalExtensions
    {
        // Wrap NSNotification / UIKeyboard observer tokens (NSObject) as IDisposable
        public static System.IDisposable AsDisposable(this NSObject token)
            => new ActionDisposable(() => token?.Dispose());
    }
}
#endif