using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FlockForge.Helpers;

public static class ResourceFixes
{
    public static void NormalizeOnIdiomDouble(string key, double fallback)
    {
        var res = Application.Current?.Resources;
        if (res is null) return;

        if (!res.ContainsKey(key))
        {
            System.Diagnostics.Debug.WriteLine($"[ResourceFixes] Key '{key}' not found in resources");
            return;
        }

        if (res.TryGetValue(key, out var value) && value is OnIdiom<double> onIdiom)
        {
            double resolved;
            
            if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                resolved = onIdiom.Phone;
            else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
                resolved = onIdiom.Tablet;
            else if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                resolved = onIdiom.Desktop;
            else if (DeviceInfo.Idiom == DeviceIdiom.TV)
                resolved = onIdiom.TV;
            else if (DeviceInfo.Idiom == DeviceIdiom.Watch)
                resolved = onIdiom.Watch;
            else
                resolved = fallback;

            res[key] = resolved; // overwrite with a plain double
            System.Diagnostics.Debug.WriteLine($"[ResourceFixes] '{key}' normalized to {resolved}");
        }
    }

    public static void NormalizeOnIdiomDoubles(params (string key, double fallback)[] resources)
    {
        foreach (var (key, fallback) in resources)
            NormalizeOnIdiomDouble(key, fallback);
    }
}