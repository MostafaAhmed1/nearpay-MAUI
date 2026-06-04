using System.Reflection;

namespace NearpayPosMauiDemo.App.Platforms.Android.Services;

internal static class NearpaySdkRawDump
{
    public static string Explain(string operation, object? obj)
    {
        var raw = Dump(obj);
        var headline = string.IsNullOrWhiteSpace(operation)
            ? "NearPay: فشل تنفيذ العملية."
            : $"NearPay: فشل تنفيذ العملية ({operation}).";

        var extracted = ExtractInterestingText(obj);
        var details = string.IsNullOrWhiteSpace(extracted) ? "(لا توجد تفاصيل نصية من الـ SDK)" : extracted;

        var causes =
            "أسباب شائعة:\n" +
            "- Package Name غير مسجّل في NearPay Dashboard (Apps).\n" +
            "- المستخدم غير مدعو على الـ Terminal أو لم يقبل الدعوة (Terminals → Access → Invite user).\n" +
            "- تم تثبيت Payment Plugin لكن نسخة البيئة غير مطابقة (Sandbox vs Production).\n" +
            "- صلاحيات الجهاز غير مكتملة (Location / Phone state) أو خدمة الموقع غير مفعلة.\n" +
            "- الجهاز غير متوافق (لا يدعم NFC / Root / قيود الشركة/MDM).\n" +
            "- اتصال الإنترنت غير متوفر أو محجوب.";

        return string.Join("\n\n", headline, details, causes, "Raw:\n" + raw);
    }

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

    private static string ExtractInterestingText(object? obj)
    {
        if (obj is null) return "";

        var t = obj.GetType();
        var candidates = new List<string>();

        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead) continue;
            if (p.GetIndexParameters().Length > 0) continue;

            var name = p.Name;
            if (!LooksLikeMessageProperty(name)) continue;

            object? val;
            try { val = p.GetValue(obj); }
            catch { continue; }

            if (val is null) continue;
            var s = val.ToString();
            if (string.IsNullOrWhiteSpace(s)) continue;
            candidates.Add($"{name}: {s}");
        }

        if (candidates.Count == 0) return "";
        return string.Join("\n", candidates);
    }

    private static bool LooksLikeMessageProperty(string name)
    {
        var n = name.Trim();
        if (n.Length == 0) return false;

        return n.Equals("Message", StringComparison.OrdinalIgnoreCase)
               || n.Equals("Error", StringComparison.OrdinalIgnoreCase)
               || n.Equals("ErrorMessage", StringComparison.OrdinalIgnoreCase)
               || n.Equals("Reason", StringComparison.OrdinalIgnoreCase)
               || n.Equals("Description", StringComparison.OrdinalIgnoreCase)
               || n.Equals("Details", StringComparison.OrdinalIgnoreCase)
               || n.Equals("Code", StringComparison.OrdinalIgnoreCase)
               || n.Equals("Status", StringComparison.OrdinalIgnoreCase)
               || n.Equals("Type", StringComparison.OrdinalIgnoreCase);
    }
}
