using Android.App;
using Android.Net;
using Android.Nfc;
using Android.Provider;
using AndroidX.Core.Content;
using NearpayPosMauiDemo.Core.Abstractions;
using NearpayPosMauiDemo.Core.Models;

using IO.Nearpay.Sdk;
using IO.Nearpay.Sdk.Utils;
using IO.Nearpay.Sdk.Utils.Enums;
using IO.Nearpay.Sdk.Utils.Listeners;
using JLocale = Java.Util.Locale;
using Java.Util;
using Microsoft.Maui.ApplicationModel;

namespace NearpayPosMauiDemo.App.Platforms.Android.Services;

public sealed class NearpayServiceAndroid : INearpayService
{
    private NearPay? _nearPay;

    public bool IsInitialized => _nearPay is not null;

    public Task InitializeAsync(NearpayInitializationRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // مهم: NearPay/Payment Plugin يعتمد على Activity context لفتح واجهات/Activities.
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("لا يوجد Activity حالي. جرّب إعادة فتح التطبيق ثم المحاولة مرة أخرى.");

        var builder = new NearPay.Builder()
            .Context(activity)
            .Environment(MapEnvironment(request.Environment))
            .AuthenticationData(MapAuth(request))
            .Locale(ToLocale(request.Locale))
            .LoadingUi(true);

        // Optional: customize payment text (Arabic/English)
        builder.PaymentText(new PaymentText("يرجى تمرير البطاقة", "please tap your card"));

        _nearPay = builder.Build();
        return Task.CompletedTask;
    }

    public async Task<NearpayOperationResult> SetupAsync(CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        // فحص متطلبات الجهاز قبل Setup لتفادي "GeneralFailure" الغامضة
        if (Platform.CurrentActivity is not Activity activity)
            return new NearpayOperationResult(false, "لا يوجد Activity حالي. أغلق التطبيق وافتحه ثم حاول مرة أخرى.");

        var issues = GetPreflightIssues(activity);
        if (issues.Count > 0)
            return new NearpayOperationResult(false, "تعذر تسجيل الجهاز بسبب:\n- " + string.Join("\n- ", issues));

        var tcs = new TaskCompletionSource<NearpayOperationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        nearPay.Setup(new SetupListener(tcs));
        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> PurchaseAsync(NearpayPurchaseRequest request, CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        nearPay.Purchase(
            amount: request.AmountMinor,
            customerReferenceNumber: request.CustomerReferenceNumber,
            enableReceiptUi: request.EnableReceiptUi,
            enableReversal: request.EnableReversal,
            finishTimeOut: request.FinishTimeoutSeconds,
            requestId: request.RequestId is null ? null : UUID.FromString(request.RequestId.Value.ToString()),
            enableUiDismiss: request.EnableUiDismiss,
            listener: new PurchaseListener(tcs));

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> RefundAsync(NearpayRefundRequest request, CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        nearPay.Refund(
            amount: request.AmountMinor,
            transactionUuid: request.TransactionUuid,
            customerReferenceNumber: request.CustomerReferenceNumber,
            enableReceiptUi: request.EnableReceiptUi,
            enableReversal: request.EnableReversal,
            enableEditableRefundAmountUi: request.EnableEditableRefundAmountUi,
            finishTimeOut: request.FinishTimeoutSeconds,
            requestId: request.RequestId is null ? null : UUID.FromString(request.RequestId.Value.ToString()),
            adminPin: request.AdminPin,
            enableUiDismiss: request.EnableUiDismiss,
            listener: new RefundListener(tcs));

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> ReverseAsync(NearpayReverseRequest request, CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        nearPay.Reverse(
            transactionUuid: request.TransactionUuid,
            enableReceiptUi: request.EnableReceiptUi,
            finishTimeOut: request.FinishTimeoutSeconds,
            enableUiDismiss: request.EnableUiDismiss,
            listener: new ReversalListener(tcs));

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayReconcileResult>> ReconcileAsync(NearpayReconcileRequest request, CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<NearpayReconcileResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        nearPay.Reconcile(
            reconcileId: request.ReconcileId is null ? null : UUID.FromString(request.ReconcileId.Value.ToString()),
            enableReceiptUi: request.EnableReceiptUi,
            adminPin: request.AdminPin,
            finishTimeOut: request.FinishTimeoutSeconds,
            enableUiDismiss: request.EnableUiDismiss,
            listener: new ReconcileListener(tcs));

        return await tcs.Task.ConfigureAwait(false);
    }

    private NearPay EnsureInitialized()
        => _nearPay ?? throw new InvalidOperationException("NearPay غير مهيأ بعد. اضغط Initialize أولاً.");

    private static Environments MapEnvironment(NearpayEnvironment env) => env switch
    {
        NearpayEnvironment.Production => Environments.Production!,
        NearpayEnvironment.Staging => Environments.Staging!,
        NearpayEnvironment.Testing => Environments.Testing!,
        _ => Environments.Sandbox!,
    };

    private static AuthenticationData MapAuth(NearpayInitializationRequest request)
    {
        var value = request.AuthValue?.Trim();
        return request.AuthMode switch
        {
            NearpayAuthMode.Jwt => new AuthenticationData.Jwt(value ?? string.Empty),
            NearpayAuthMode.Email => new AuthenticationData.Email(value ?? string.Empty),
            NearpayAuthMode.Mobile => new AuthenticationData.Mobile(value ?? string.Empty),

            NearpayAuthMode.DeviceLinking => AuthenticationData.DeviceLinking.Instance,

            _ => AuthenticationData.UserEnter.Instance,
        };
    }

    private static JLocale ToLocale(string? localeTag)
        => string.IsNullOrWhiteSpace(localeTag)
            ? JLocale.Default!
            : JLocale.ForLanguageTag(localeTag);

    // -----------------
    // Listener adapters
    // -----------------

    private sealed class SetupListener(TaskCompletionSource<NearpayOperationResult> tcs)
        : Java.Lang.Object, ISetupListener
    {
        public void OnSetupCompleted()
            => tcs.TrySetResult(new NearpayOperationResult(true, "تم تسجيل الجهاز بنجاح"));

        public void OnSetupFailed(SetupFailure setupFailure)
            => tcs.TrySetResult(new NearpayOperationResult(false, DescribeSetupFailure(setupFailure)));
    }

    private sealed class PurchaseListener(TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>> tcs)
        : Java.Lang.Object, IPurchaseListener
    {
        public void OnPurchaseApproved(TransactionData transactionData)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                true,
                "Approved",
                new NearpayTransactionResult(transactionData.ToString() ?? "Approved")));

        public void OnPurchaseFailed(PurchaseFailure purchaseFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                false,
                purchaseFailure.ToString() ?? "Purchase failed"));
    }

    private sealed class RefundListener(TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>> tcs)
        : Java.Lang.Object, IRefundListener
    {
        public void OnRefundApproved(TransactionData transactionData)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                true,
                "Approved",
                new NearpayTransactionResult(transactionData.ToString() ?? "Approved")));

        public void OnRefundFailed(RefundFailure refundFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                false,
                refundFailure.ToString() ?? "Refund failed"));
    }

    private sealed class ReversalListener(TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>> tcs)
        : Java.Lang.Object, IReversalListener
    {
        public void OnReversalFinished(TransactionData transactionData)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                true,
                "Finished",
                new NearpayTransactionResult(transactionData.ToString() ?? "Finished")));

        public void OnReversalFailed(ReversalFailure reversalFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                false,
                reversalFailure.ToString() ?? "Reverse failed"));
    }

    private sealed class ReconcileListener(TaskCompletionSource<NearpayOperationResult<NearpayReconcileResult>> tcs)
        : Java.Lang.Object, IReconcileListener
    {
        public void OnReconcileFinished(IO.Nearpay.Sdk.Data.Models.ReconciliationReceipt? receipt)
        {
            var json = receipt is null ? null : ReceiptUtilsKt.ToJson(receipt);
            tcs.TrySetResult(new NearpayOperationResult<NearpayReconcileResult>(
                true,
                "Finished",
                new NearpayReconcileResult(receipt?.ToString() ?? "Finished", json)));
        }

        public void OnReconcileFailed(ReconcileFailure reconcileFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayReconcileResult>(
                false,
                reconcileFailure.ToString() ?? "Reconcile failed"));
    }

    private static string DescribeSetupFailure(SetupFailure failure)
    {
        return failure switch
        {
            SetupFailure.NotInstalled => "لم يتم العثور على NearPay Payment Plugin. ثبّت/حدّث الـ Plugin ثم أعد المحاولة.",
            SetupFailure.AlreadyInstalled => "Payment Plugin مثبت بالفعل. جرّب إعادة فتح التطبيق ثم تسجيل الجهاز مرة أخرى.",
            SetupFailure.AuthenticationFailed auth => $"فشل التوثيق: {auth.Message}",
            SetupFailure.InvalidStatus invalid => "حالة الجهاز غير مناسبة:\n- " +
                                                 string.Join("\n- ", invalid.Status.Select(MapStatusCheckError)),
            SetupFailure.GeneralFailure => "فشل عام أثناء Setup. (غالباً بسبب اتصال/صلاحيات/إعدادات Dashboard). جرّب زر (مساعدة) داخل التطبيق واتبع النقاط.",
            _ => failure.ToString() ?? "Setup failed"
        };
    }

    private static string MapStatusCheckError(StatusCheckError err)
        => err?.ToString() switch
        {
            "CONNECTIVITY_UNAVAILABLE" => "لا يوجد اتصال إنترنت فعّال.",
            "VPN_DETECTED" => "تم اكتشاف VPN. أوقف الـ VPN ثم أعد المحاولة.",
            "DEV_MODE_ON" => "وضع المطوّر (Developer options) مفعّل. أوقفه ثم أعد المحاولة.",
            "LOCATION_PERMISSION_MISSING" => "صلاحية الموقع غير مُعطاة للتطبيق.",
            "LOCATION_MISSING" => "خدمة الموقع (Location) غير مفعّلة.",
            "NFC_DISABLED" => "NFC غير مفعّل.",
            "NFC_NOT_FOUND" => "لا يوجد NFC في هذا الجهاز.",
            "NOT_INSTALLED" => "Payment Plugin غير مثبت.",
            "UPDATED_REQUIRED" => "يلزم تحديث Payment Plugin.",
            "NOT_SECURE" => "الجهاز غير آمن (قد يكون Root/Bootloader/إعدادات أمان).",
            "UNSUPPORTED_DEVICE" => "الجهاز غير مدعوم.",
            "UNSUPPORTED_SDK_VERSION" => "إصدار SDK/Android غير مدعوم.",
            "OPERATION_NOT_SUPPORTED" => "العملية غير مدعومة على هذا الجهاز.",
            "TERMINAL_UPDATING" => "الـ Terminal في حالة تحديث. انتظر ثم أعد المحاولة.",
            "TERMINAL_RECONCILING" => "الـ Terminal في حالة تسوية. أنهِ التسوية ثم أعد المحاولة.",
            "PHONE_STATE_DISABLED" => "إعدادات Phone State/Sim غير مناسبة على الجهاز.",
            _ => err?.ToString() ?? "Unknown status error"
        };

    private static List<string> GetPreflightIssues(Activity activity)
    {
        var issues = new List<string>();

        // NFC
        var nfc = NfcAdapter.GetDefaultAdapter(activity);
        if (nfc is null)
            issues.Add("الجهاز لا يدعم NFC (مطلوب للدفع Tap).");
        else if (!nfc.IsEnabled)
            issues.Add("NFC مقفول. فعّله من إعدادات الجهاز.");

        // Connectivity + VPN
        var cm = (ConnectivityManager?)activity.GetSystemService(global::Android.Content.Context.ConnectivityService);
        var network = cm?.ActiveNetwork;
        var caps = network is null ? null : cm?.GetNetworkCapabilities(network);
        if (caps is null || !caps.HasCapability(NetCapability.Internet))
            issues.Add("لا يوجد اتصال إنترنت فعّال.");
        if (caps?.HasTransport(TransportType.Vpn) == true)
            issues.Add("VPN مفعّل. أوقف VPN ثم أعد المحاولة.");

        // Developer options
        try
        {
            var dev = Settings.Global.GetInt(activity.ContentResolver, Settings.Global.DevelopmentSettingsEnabled, 0);
            if (dev == 1)
                issues.Add("Developer options مفعّل. أوقفه ثم أعد المحاولة.");
        }
        catch
        {
            // ignore
        }

        // Location permission (سبب شائع لفشل Setup على بعض الأجهزة)
        try
        {
            var granted = ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.AccessFineLocation)
                          == global::Android.Content.PM.Permission.Granted;
            if (!granted)
                issues.Add("صلاحية الموقع غير مُعطاة للتطبيق. امنح Location permission ثم أعد المحاولة.");
        }
        catch
        {
            // ignore
        }

        return issues;
    }
}
