using System.Reflection;

namespace NearpayPosMauiDemo.App.Platforms.Android.Services;

internal static class NearpaySdkRawDump
{
    public static string Dump(object? obj)
    {
        if (obj is null) return "(null)";

        var t = obj.GetType();
        var lines = new List<string>
        {
            $"Type: {t.FullName}",
            $"ToString: {obj}"
        };

        // Print public readable properties (raw values from SDK objects)
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead) continue;
            if (p.GetIndexParameters().Length > 0) continue;

            object? val;
            try { val = p.GetValue(obj); }
            catch { continue; }

            if (val is null) continue;

            if (val is string s)
            {
                if (!string.IsNullOrWhiteSpace(s))
                    lines.Add($"{p.Name}: {s}");
                continue;
            }

            if (val is System.Collections.IEnumerable en && val is not string)
            {
                var items = new List<string>();
                foreach (var it in en)
                {
                    if (it is null) continue;
                    items.Add(it.ToString() ?? it.GetType().Name);
                }
                if (items.Count > 0)
                    lines.Add($"{p.Name}: [{string.Join(", ", items)}]");
                continue;
            }

            if (val is ValueType)
            {
                lines.Add($"{p.Name}: {val}");
                continue;
            }
        }

        return string.Join("\n", lines);
    }
}

