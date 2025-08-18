using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace FlockForge.Helpers
{
    internal static class ResourceTypeValidator
    {
#if DEBUG
        public static bool Expect<T>(string key)
        {
            if (!Application.Current?.Resources?.TryGetValue(key, out var obj) ?? true)
            {
                Debug.WriteLine($"Resource missing: {key}");
                return false;
            }
            if (obj is T) return true;
            Debug.WriteLine($"Type mismatch for {key}: expected {typeof(T).Name}, got {obj?.GetType().Name ?? "null"}");
            return false;
        }

        public static void ValidateAll()
        {
            Expect<Thickness>("GF.Padding");
            Expect<Thickness>("GF.Margin");
            Expect<double>("GF.Spacing");
            Expect<double>("GF.HeightRequest");
            Expect<double>("GF.MinHeightRequest");
            Expect<double>("GF.WidthRequest");
            Expect<double>("GF.FontSize");
            Expect<int>("GF.Corner.Button");
            Expect<float>("GF.Corner.Frame");
            Expect<CornerRadius>("GF.Corner.Border");
        }
#endif
    }
}
