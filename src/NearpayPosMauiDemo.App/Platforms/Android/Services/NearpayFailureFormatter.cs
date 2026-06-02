using IO.Nearpay.Sdk.Utils.Enums;

namespace NearpayPosMauiDemo.App.Platforms.Android.Services;

internal static class NearpayFailureFormatter
{
    public static string Describe(SetupFailure failure)
        => failure switch
        {
            SetupFailure.NotInstalled => "Payment Plugin غير مثبت.",
            SetupFailure.AlreadyInstalled => "Payment Plugin مثبت بالفعل.",
            SetupFailure.AuthenticationFailed auth => $"فشل التوثيق: {auth.Message}",
            SetupFailure.InvalidStatus invalid => "حالة الجهاز غير مناسبة:\n- " + string.Join("\n- ", invalid.Status.Select(MapStatusCheckError)),
            SetupFailure.GeneralFailure => "فشل عام أثناء Setup (تحقق من إعدادات الجهاز/الاتصال/الدعوات/الـ Terminal).",
            _ => failure.ToString() ?? "Setup failed"
        };

    public static string Describe(PurchaseFailure failure)
        => failure switch
        {
            PurchaseFailure.AuthenticationFailed auth => $"فشل التوثيق: {auth.Message}",
            PurchaseFailure.InvalidStatus invalid => "حالة الجهاز غير مناسبة:\n- " + string.Join("\n- ", invalid.Status.Select(MapStatusCheckError)),
            PurchaseFailure.PurchaseRejected rej => $"تم رفض العملية: {rej.Message}",
            PurchaseFailure.PurchaseDeclined dec => $"العملية مرفوضة من المصدر (Declined): {SafeTx(dec.TransactionData)}",
            PurchaseFailure.GeneralFailure => "فشل عام أثناء الدفع.",
            _ => failure.ToString() ?? "Purchase failed"
        };

    public static string Describe(RefundFailure failure)
        => failure switch
        {
            RefundFailure.AuthenticationFailed auth => $"فشل التوثيق: {auth.Message}",
            RefundFailure.InvalidStatus invalid => "حالة الجهاز غير مناسبة:\n- " + string.Join("\n- ", invalid.Status.Select(MapStatusCheckError)),
            RefundFailure.RefundRejected rej => $"تم رفض الاسترجاع: {rej.Message}",
            RefundFailure.RefundDeclined dec => $"الاسترجاع مرفوض من المصدر (Declined): {SafeTx(dec.TransactionData)}",
            RefundFailure.GeneralFailure => "فشل عام أثناء الاسترجاع.",
            _ => failure.ToString() ?? "Refund failed"
        };

    public static string Describe(ReversalFailure failure)
        => failure switch
        {
            ReversalFailure.AuthenticationFailed auth => $"فشل التوثيق: {auth.Message}",
            ReversalFailure.InvalidStatus invalid => "حالة الجهاز غير مناسبة:\n- " + string.Join("\n- ", invalid.Status.Select(MapStatusCheckError)),
            ReversalFailure.FailureMessage msg => $"فشل عملية العكس: {msg.Message}",
            ReversalFailure.GeneralFailure => "فشل عام أثناء العكس.",
            _ => failure.ToString() ?? "Reverse failed"
        };

    public static string Describe(ReconcileFailure failure)
        => failure switch
        {
            ReconcileFailure.AuthenticationFailed auth => $"فشل التوثيق: {auth.Message}",
            ReconcileFailure.InvalidStatus invalid => "حالة الجهاز غير مناسبة:\n- " + string.Join("\n- ", invalid.Status.Select(MapStatusCheckError)),
            ReconcileFailure.InvalidAdminPin pin => $"PIN غير صحيح: {pin.Message}",
            ReconcileFailure.FailureMessage msg => $"فشل التسوية: {msg.Message}",
            ReconcileFailure.GeneralFailure => "فشل عام أثناء التسوية.",
            _ => failure.ToString() ?? "Reconcile failed"
        };

    public static string Describe(SessionFailure failure)
        => failure switch
        {
            SessionFailure.AuthenticationFailed auth => $"فشل التوثيق: {auth.Message}",
            SessionFailure.InvalidStatus invalid => "حالة الجهاز غير مناسبة:\n- " + string.Join("\n- ", invalid.Status.Select(MapStatusCheckError)),
            SessionFailure.FailureMessage msg => $"فشل الجلسة: {msg.Message}",
            SessionFailure.GeneralFailure => "فشل عام أثناء فحص الجلسة.",
            _ => failure.ToString() ?? "Session failed"
        };

    public static string Describe(CompatibilityFailure failure)
        => failure switch
        {
            CompatibilityFailure.Incompatible inc => "الجهاز غير متوافق:\n- " + string.Join("\n- ", inc.List.Select(MapDeviceCompatibility)),
            CompatibilityFailure.GeneralFailure => "فشل عام أثناء فحص توافق الجهاز.",
            _ => failure.ToString() ?? "Compatibility failed"
        };

    private static string SafeTx(TransactionData? tx)
        => tx is null ? "(بدون بيانات)" : tx.ToString() ?? "(TransactionData)";

    private static string MapDeviceCompatibility(DeviceCompatibility dc)
        => dc?.ToString() switch
        {
            "UNSUPPORTED_NFC" => "الجهاز لا يدعم NFC.",
            "UNSUPPORTED_ANDROID_SDK" => "إصدار Android غير مدعوم.",
            _ => dc?.ToString() ?? "Unknown"
        };

    internal static string MapStatusCheckError(StatusCheckError err)
        => err?.ToString() switch
        {
            "CONNECTIVITY_UNAVAILABLE" => "لا يوجد اتصال إنترنت فعّال.",
            "VPN_DETECTED" => "تم اكتشاف VPN. أوقف الـ VPN ثم أعد المحاولة.",
            "DEV_MODE_ON" => "Developer options مفعّل. NearPay قد يرفض العمل لهذا السبب.",
            "LOCATION_PERMISSION_MISSING" => "صلاحية الموقع غير مُعطاة للتطبيق.",
            "LOCATION_MISSING" => "خدمة الموقع (Location) غير مفعّلة.",
            "NFC_DISABLED" => "NFC غير مفعّل.",
            "NFC_NOT_FOUND" => "لا يوجد NFC في هذا الجهاز.",
            "NOT_INSTALLED" => "Payment Plugin غير مثبت.",
            "UPDATED_REQUIRED" => "يلزم تحديث Payment Plugin.",
            "NOT_SECURE" => "الجهاز غير آمن (Root/Bootloader/إعدادات أمان).",
            "UNSUPPORTED_DEVICE" => "الجهاز غير مدعوم.",
            "UNSUPPORTED_SDK_VERSION" => "إصدار SDK/Android غير مدعوم.",
            "OPERATION_NOT_SUPPORTED" => "العملية غير مدعومة على هذا الجهاز.",
            "TERMINAL_UPDATING" => "الـ Terminal في حالة تحديث. انتظر ثم أعد المحاولة.",
            "TERMINAL_RECONCILING" => "الـ Terminal في حالة تسوية. أنهِ التسوية ثم أعد المحاولة.",
            "PHONE_STATE_DISABLED" => "إعدادات Phone State/Sim غير مناسبة على الجهاز.",
            _ => err?.ToString() ?? "Unknown status error"
        };
}

