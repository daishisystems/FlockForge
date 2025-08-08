using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace FlockForge.Helpers
{
    public static class UIThreadHelper
    {
        public static async Task RunOffMainThreadAsync(Func<Task> action)
        {
            await Task.Run(action).ConfigureAwait(false);
        }

        public static async Task<T> RunOffMainThreadAsync<T>(Func<Task<T>> action)
        {
            return await Task.Run(action).ConfigureAwait(false);
        }

        public static async Task UpdateUIAsync(Action action)
        {
            await MainThread.InvokeOnMainThreadAsync(action);
        }
    }
}