using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FlockForge.Services;

public static class GloveModeService
{
    public static bool IsOn { get; private set; }

    public static void Set(bool on)
    {
        IsOn = on;
        var r = Application.Current?.Resources;
        if (r is null) return;

        r["GF.Padding"] = on ? 24d : 16d;
        r["GF.Spacing"] = on ? 24d : 16d;
        r["GF.MinControlHeight"] = on ? 64d : (DeviceInfo.Idiom == DeviceIdiom.Phone ? 56d : 60d);
        r["GF.TileHeight"] = on ? 120d : (DeviceInfo.Idiom == DeviceIdiom.Phone ? 96d : 108d);
    }
}
