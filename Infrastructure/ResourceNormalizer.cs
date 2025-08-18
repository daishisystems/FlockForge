using System;
using System.Linq;
using System.Reflection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FlockForge.Infrastructure
{
    /// <summary>
    /// Resolves OnIdiom<T>, OnAppTheme<T>, OnPlatform<T> into concrete values
    /// and normalizes nested values within resource objects and style setters.
    /// </summary>
    public static class ResourceNormalizer
    {
        public static void NormalizeAll(Application app)
        {
            if (app?.Resources == null) return;
            NormalizeDictionary(app.Resources);

            // Re-run when theme changes (so OnAppTheme<T> stays correct)
            app.RequestedThemeChanged -= OnRequestedThemeChanged;
            app.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        private static void OnRequestedThemeChanged(object sender, AppThemeChangedEventArgs e)
        {
            if (sender is Application app && app.Resources != null)
                NormalizeDictionary(app.Resources);
        }

        private static void NormalizeDictionary(ResourceDictionary dict)
        {
            if (dict == null) return;

            // 1) Normalize top-level keys
            foreach (var key in dict.Keys.ToList())
            {
                var current = dict[key];
                var normalized = ResolveSpecialValues(current);

                if (!ReferenceEquals(current, normalized))
                    dict[key] = normalized;

                if (dict[key] != null)
                    NormalizeObjectProperties(dict[key]);
            }

            // 2) Normalize Styles' Setters
            foreach (var style in dict.Values.OfType<Style>())
                NormalizeStyle(style);

            // 3) Recurse into MergedDictionaries
            foreach (var merged in dict.MergedDictionaries)
                NormalizeDictionary(merged);
        }

        private static void NormalizeStyle(Style style)
        {
            if (style == null) return;

            foreach (var setter in style.Setters.ToList())
            {
                var val = setter.Value;
                var normalized = ResolveSpecialValues(val);
                if (!ReferenceEquals(val, normalized))
                    setter.Value = normalized;

                if (setter.Value != null)
                    NormalizeObjectProperties(setter.Value);
            }
        }

        /// <summary>
        /// Shallow graph normalization of nested object properties
        /// so embedded values like FontImageSource.Size = {OnIdiom ...} get resolved.
        /// </summary>
        private static void NormalizeObjectProperties(object obj, int depth = 0)
        {
            if (obj == null || depth > 2) return;

            var type = obj.GetType();
            if (type.IsPrimitive || obj is string) return;

            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || !p.CanWrite) continue;

                object cur;
                try { cur = p.GetValue(obj); }
                catch { continue; }

                if (cur == null) continue;

                var normalized = ResolveSpecialValues(cur);
                if (!ReferenceEquals(cur, normalized))
                {
                    try { p.SetValue(obj, normalized); } catch { /* ignore */ }
                }
                else
                {
                    NormalizeObjectProperties(cur, depth + 1);
                }
            }
        }

        private static object ResolveSpecialValues(object value)
        {
            if (value == null) return null;
            var t = value.GetType();

            // OnIdiom<T>
            if (t.IsGenericType && t.Name.StartsWith("OnIdiom`", StringComparison.Ordinal))
                return ResolveOnIdiom(value);

            // OnAppTheme<T>
            if (t.IsGenericType && t.Name.StartsWith("OnAppTheme`", StringComparison.Ordinal))
                return ResolveOnAppTheme(value);

            // OnPlatform<T>
            if (t.IsGenericType && t.Name.StartsWith("OnPlatform`", StringComparison.Ordinal))
                return ResolveOnPlatform(value);

            return value;
        }

        private static object ResolveOnIdiom(object onIdiom)
        {
            var t = onIdiom.GetType();
            object get(string name) =>
                t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(onIdiom);

            var idiom = DeviceInfo.Idiom;
            object chosen = null;

            switch (idiom)
            {
                case DeviceIdiom.Phone:   chosen = get("Phone"); break;
                case DeviceIdiom.Tablet:  chosen = get("Tablet"); break;
                case DeviceIdiom.Desktop: chosen = get("Desktop"); break;
                case DeviceIdiom.TV:      chosen = get("TV"); break;
                case DeviceIdiom.Watch:   chosen = get("Watch"); break;
                default:                  chosen = null; break;
            }

            return chosen ?? get("Default") ?? onIdiom;
        }

        private static object ResolveOnAppTheme(object onTheme)
        {
            var t = onTheme.GetType();
            object get(string name) =>
                t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(onTheme);

            var theme = Application.Current?.RequestedTheme ?? AppTheme.Unspecified;

            return theme switch
            {
                AppTheme.Dark  => get("Dark")   ?? get("Default") ?? onTheme,
                AppTheme.Light => get("Light")  ?? get("Default") ?? onTheme,
                _              => get("Default") ?? onTheme
            };
        }

        private static object ResolveOnPlatform(object onPlatform)
        {
            var t = onPlatform.GetType();
            object get(string name) =>
                t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(onPlatform);

            object pick(params string[] names)
            {
                foreach (var n in names)
                {
                    var v = get(n);
                    if (v is not null) return v;
                }
                return null;
            }

#if ANDROID
            return pick("Android", "Default") ?? onPlatform;
#elif IOS
            return pick("iOS", "Default") ?? onPlatform;
#elif MACCATALYST
            return pick("MacCatalyst", "iOS", "Default") ?? onPlatform;
#elif WINDOWS
            return pick("WinUI", "Windows", "Default") ?? onPlatform;
#else
            return pick("Default") ?? onPlatform;
#endif
        }
    }
}

