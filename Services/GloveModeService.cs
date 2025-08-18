using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FlockForge.Services;

public static class GloveModeService
{
    public static bool IsOn { get; private set; }

    public static void Set(bool on)
    {
        IsOn = on;
        OnUI(() =>
        {
            var r = Application.Current?.Resources;
            if (r is null) return;

            r["GF.Padding"] = on ? new Thickness(24) : new Thickness(16);
            r["GF.Spacing"] = on ? 16d : 8d;
            r["GF.MinHeightRequest"] = on ? 64d : (DeviceInfo.Idiom == DeviceIdiom.Phone ? 56d : 60d);
            r["GF.TileHeight"] = on ? 120d : (DeviceInfo.Idiom == DeviceIdiom.Phone ? 96d : 108d);
        });
    }

    static void OnUI(Action action)
    {
        if (MainThread.IsMainThread) action();
        else MainThread.BeginInvokeOnMainThread(action);
    }
}
