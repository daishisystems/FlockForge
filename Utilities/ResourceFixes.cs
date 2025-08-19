using System;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FlockForge.Utilities
{
    public static class ResourceFixes
    {
        private const string Tag = "[ResourceFixes]";

        public static void NormalizeOnIdiomDoubles(params (string key, double fallback)[] resources)
        {
            foreach (var (key, fallback) in resources)
                NormalizeOnIdiomDouble(key, fallback);
        }

        public static void NormalizeOnIdiomDouble(string key, double fallback)
        {
            try
            {
                var res = Application.Current?.Resources;
                if (res is null)
                {
                    System.Diagnostics.Debug.WriteLine($"{Tag} Application.Current.Resources is null; skipping.");
                    return;
                }

                if (!res.ContainsKey(key))
                {
                    System.Diagnostics.Debug.WriteLine($"{Tag} Key '{key}' not found; skipping.");
                    return;
                }

                // Early exit if already normalized
                if (res[key] is double already)
                {
                    System.Diagnostics.Debug.WriteLine($"{Tag} '{key}' already normalized => {already}");
                    return;
                }

                var original = res[key];
                var resolved = ResolveToDouble(original, fallback);
                res[key] = resolved;

                System.Diagnostics.Debug.WriteLine($"{Tag} '{key}': {Describe(original)} -> double {resolved}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{Tag} Failed to normalize '{key}': {ex}");
            }
        }

        private static string Describe(object? v) => v switch
        {
            null => "null",
            OnIdiom<double> => "OnIdiom<double>",
            OnIdiom<object> => "OnIdiom<object>",
            _ => v.GetType().Name
        };

        private static double ResolveToDouble(object? value, double fallback)
        {
            if (value is null) return fallback;

            // Exact match: OnIdiom<double>
            if (value is OnIdiom<double> od)
                return PickIdiom(od.Phone, od.Tablet, od.Desktop, od.TV, od.Watch, od.Default, fallback);

            // Common case: OnIdiom<object>
            if (value is OnIdiom<object> oo)
            {
                var chosen = PickIdiomObj(oo.Phone, oo.Tablet, oo.Desktop, oo.TV, oo.Watch, oo.Default, null);
                return ConvertAnyToDouble(chosen, fallback);
            }

            // Anything else convertible
            return ConvertAnyToDouble(value, fallback);
        }

        // Value-type overload: returns a double directly (no null-coalescing)
        private static double PickIdiom(
            double phone, double tablet, double desktop, double tv, double watch, double @default, double fallback)
        {
            return DeviceInfo.Idiom switch
            {
                DeviceIdiom.Phone   => phone,
                DeviceIdiom.Tablet  => tablet,
                DeviceIdiom.Desktop => desktop,
                DeviceIdiom.TV      => tv,
                DeviceIdiom.Watch   => watch,
                _                   => @default
            };
        }

        // Reference-type overload: may return null; caller will handle fallback
        private static object? PickIdiomObj(
            object? phone, object? tablet, object? desktop, object? tv, object? watch, object? @default, object? fallback)
        {
            var chosen = DeviceInfo.Idiom switch
            {
                DeviceIdiom.Phone   => phone,
                DeviceIdiom.Tablet  => tablet,
                DeviceIdiom.Desktop => desktop,
                DeviceIdiom.TV      => tv,
                DeviceIdiom.Watch   => watch,
                _                   => @default
            };
            return chosen ?? fallback;
        }

        private static double ConvertAnyToDouble(object? value, double fallback)
        {
            try
            {
                switch (value)
                {
                    case null: return fallback;
                    case double d: return d;
                    case float f: return f;
                    case int i: return i;
                    case long l: return l;
                    case decimal m: return (double)m;
                    case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                        return parsed;
                    default:
                        var converted = Convert.ChangeType(value, typeof(double), CultureInfo.InvariantCulture);
                        return converted is double cd ? cd : fallback;
                }
            }
            catch
            {
                return fallback;
            }
        }
    }
}

