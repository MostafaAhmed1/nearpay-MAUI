using NearpayPosMauiDemo.Core.Abstractions;
using NearpayPosMauiDemo.Core.Models;

using IO.Nearpay.Sdk;
using IO.Nearpay.Sdk.Data.Models;
using IO.Nearpay.Sdk.Utils;
using IO.Nearpay.Sdk.Utils.Enums;
using IO.Nearpay.Sdk.Utils.Listeners;
using Java.Util;
using JLocale = Java.Util.Locale;
using Microsoft.Maui.ApplicationModel;

namespace NearpayPosMauiDemo.App.Platforms.Android.Services;

public sealed class NearpayServiceAndroid : INearpayService
{
    private NearPay? _nearPay;
    private NearpayEnvironment? _lastEnvironment;

    public bool IsInitialized => _nearPay is not null;

    public async Task InitializeAsync(NearpayInitializationRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ValidateRequest(request);
        _lastEnvironment = request.Environment;

        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("لا يوجد Activity حالي. جرّب إعادة فتح التطبيق ثم المحاولة مرة أخرى.");

        var builder = new NearPay.Builder()
            .Context(activity)
            .Environment(MapEnvironment(request.Environment))
            .AuthenticationData(MapAuth(request))
            .Locale(ToLocale(request.Locale));

        _nearPay = builder.Build();
    }

    public async Task<NearpayOperationResult> PrepareAsync(
        NearpayInitializationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            await InitializeAsync(request, ct).ConfigureAwait(false);
            return await SetupAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Fail("Prepare", ex);
        }
    }

    public async Task<NearpayOperationResult> SetupAsync(CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();
        var activity = Platform.CurrentActivity;
        if (activity is not null)
        {
            var preflight = TryPreflightPaymentPlugin(activity);
            if (preflight is not null)
                return preflight;
        }

        var tcs = CreateCompletionSource<NearpayOperationResult>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.Setup(new SetupListener(tcs)),
            ct,
            ex => Fail("Setup", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<string>> DeviceCompatibilityAsync(CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult<string>>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.DeviceCompatibility(new CompatibilityListener(tcs)),
            ct,
            ex => Fail<string>("DeviceCompatibility", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<string>> GetUserSessionAsync(CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult<string>>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.GetUserSession(new CheckSessionListener(tcs)),
            ct,
            ex => Fail<string>("GetUserSession", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult> DismissAsync(CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.Dismiss(new DismissListener(tcs)),
            ct,
            ex => Fail("Dismiss", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult> LogoutAsync(CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.Logout(new LogoutListener(tcs)),
            ct,
            ex => Fail("Logout", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayReconcileResult>> GetReconciliationReceiptAsync(
        NearpayReconciliationReceiptRequest request,
        CancellationToken ct = default)
    {
        ValidateRequired(request.ReconciliationUuid, "ReconciliationUuid");

        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult<NearpayReconcileResult>>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.GetReconciliationByUuid(
                request.ReconciliationUuid,
                request.EnableReceiptUi,
                request.FinishTimeoutSeconds,
                new GetReconcileReceiptListener(tcs)),
            ct,
            ex => Fail<NearpayReconcileResult>("GetReconciliationReceipt", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> PurchaseAsync(
        NearpayPurchaseRequest request,
        CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult<NearpayTransactionResult>>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.Purchase(
                amount: request.AmountMinor,
                customerReferenceNumber: request.CustomerReferenceNumber,
                enableReceiptUi: request.EnableReceiptUi,
                enableReversal: request.EnableReversal,
                finishTimeOut: request.FinishTimeoutSeconds,
                requestId: ToJavaUuid(request.RequestId),
                enableUiDismiss: request.EnableUiDismiss,
                listener: new PurchaseListener(tcs)),
            ct,
            ex => Fail<NearpayTransactionResult>("Purchase", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> RefundAsync(
        NearpayRefundRequest request,
        CancellationToken ct = default)
    {
        ValidateRequired(request.TransactionUuid, "TransactionUuid");

        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult<NearpayTransactionResult>>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.Refund(
                amount: request.AmountMinor,
                transactionUuid: request.TransactionUuid,
                customerReferenceNumber: request.CustomerReferenceNumber,
                enableReceiptUi: request.EnableReceiptUi,
                enableReversal: request.EnableReversal,
                enableEditableRefundAmountUi: request.EnableEditableRefundAmountUi,
                finishTimeOut: request.FinishTimeoutSeconds,
                requestId: ToJavaUuid(request.RequestId),
                adminPin: request.AdminPin,
                enableUiDismiss: request.EnableUiDismiss,
                listener: new RefundListener(tcs)),
            ct,
            ex => Fail<NearpayTransactionResult>("Refund", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> ReverseAsync(
        NearpayReverseRequest request,
        CancellationToken ct = default)
    {
        ValidateRequired(request.TransactionUuid, "TransactionUuid");

        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult<NearpayTransactionResult>>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.Reverse(
                transactionUuid: request.TransactionUuid,
                enableReceiptUi: request.EnableReceiptUi,
                finishTimeOut: request.FinishTimeoutSeconds,
                enableUiDismiss: request.EnableUiDismiss,
                listener: new ReversalListener(tcs)),
            ct,
            ex => Fail<NearpayTransactionResult>("Reverse", ex)).ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayReconcileResult>> ReconcileAsync(
        NearpayReconcileRequest request,
        CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();
        var tcs = CreateCompletionSource<NearpayOperationResult<NearpayReconcileResult>>();
        return await ExecuteOnMainThreadAsync(
            tcs,
            () => nearPay.Reconcile(
                reconcileId: ToJavaUuid(request.ReconcileId),
                enableReceiptUi: request.EnableReceiptUi,
                adminPin: request.AdminPin,
                finishTimeOut: request.FinishTimeoutSeconds,
                enableUiDismiss: request.EnableUiDismiss,
                listener: new ReconcileListener(tcs)),
            ct,
            ex => Fail<NearpayReconcileResult>("Reconcile", ex)).ConfigureAwait(false);
    }

    private NearpayOperationResult? TryPreflightPaymentPlugin(global::Android.App.Activity activity)
    {
        if (_lastEnvironment is null)
            return null;

        string? pluginPackage = null;
        try
        {
            pluginPackage = _lastEnvironment == NearpayEnvironment.Production
                ? IO.Nearpay.Sdk_internal.Data.DataEnvironments.Production!.PluginPackageName
                : IO.Nearpay.Sdk_internal.Data.DataEnvironments.Sandbox!.PluginPackageName;
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(pluginPackage))
            return null;

        try
        {
            _ = activity.PackageManager?.GetPackageInfo(pluginPackage, 0);
            return null;
        }
        catch (global::Android.Content.PM.PackageManager.NameNotFoundException)
        {
            return new NearpayOperationResult(false, $"NearPay: Payment Plugin غير مثبت ({pluginPackage}).");
        }
    }

    private NearPay EnsureInitialized()
        => _nearPay ?? throw new InvalidOperationException("NearPay غير مهيأ بعد. نفّذ Initialize أو Prepare أولاً.");

    private static TaskCompletionSource<T> CreateCompletionSource<T>()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task<T> ExecuteOnMainThreadAsync<T>(
        TaskCompletionSource<T> tcs,
        Action action,
        CancellationToken ct,
        Func<Exception, T> onException)
    {
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(onException(ex));
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

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

    private static UUID? ToJavaUuid(Guid? value)
        => value is null ? null : UUID.FromString(value.Value.ToString());

    private static NearpayTransactionResult MapTransactionResult(string fallbackMessage, TransactionData transactionData)
    {
        var raw = NearpaySdkRawDump.Dump(transactionData);
        return new NearpayTransactionResult(transactionData.ToString() ?? fallbackMessage, raw);
    }

    private static NearpayReconcileResult MapReconcileResult(string fallbackMessage, ReconciliationReceipt? receipt)
    {
        var raw = receipt is null ? null : ReceiptUtilsKt.ToJson(receipt);
        return new NearpayReconcileResult(receipt?.ToString() ?? fallbackMessage, raw);
    }

    private static NearpayOperationResult Fail(string operation, Exception ex)
        => new(false, NearpaySdkRawDump.Explain(operation, ex), ex);

    private static NearpayOperationResult<T> Fail<T>(string operation, Exception ex)
        => new(false, NearpaySdkRawDump.Explain(operation, ex), default, ex);

    private static void ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{fieldName} مطلوب.");
    }

    private static void ValidateRequest(NearpayInitializationRequest request)
    {
        var value = request.AuthValue?.Trim();
        if (request.AuthMode is NearpayAuthMode.Jwt or NearpayAuthMode.Email or NearpayAuthMode.Mobile)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("طريقة الدخول المختارة تتطلب Auth Value. اكتب JWT/Email/Mobile أو اختر UserEnter.");
        }
    }

    private sealed class SetupListener(TaskCompletionSource<NearpayOperationResult> tcs)
        : Java.Lang.Object, ISetupListener
    {
        public void OnSetupCompleted()
            => tcs.TrySetResult(new NearpayOperationResult(true, "Setup completed successfully."));

        public void OnSetupFailed(SetupFailure setupFailure)
            => tcs.TrySetResult(new NearpayOperationResult(false, NearpaySdkRawDump.Explain("Setup", setupFailure)));
    }

    private sealed class PurchaseListener(TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>> tcs)
        : Java.Lang.Object, IPurchaseListener
    {
        public void OnPurchaseApproved(TransactionData transactionData)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                true,
                "Purchase approved.",
                MapTransactionResult("Purchase approved.", transactionData)));

        public void OnPurchaseFailed(PurchaseFailure purchaseFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                false,
                NearpaySdkRawDump.Explain("Purchase", purchaseFailure)));
    }

    private sealed class RefundListener(TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>> tcs)
        : Java.Lang.Object, IRefundListener
    {
        public void OnRefundApproved(TransactionData transactionData)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                true,
                "Refund approved.",
                MapTransactionResult("Refund approved.", transactionData)));

        public void OnRefundFailed(RefundFailure refundFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                false,
                NearpaySdkRawDump.Explain("Refund", refundFailure)));
    }

    private sealed class ReversalListener(TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>> tcs)
        : Java.Lang.Object, IReversalListener
    {
        public void OnReversalFinished(TransactionData transactionData)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                true,
                "Reversal finished.",
                MapTransactionResult("Reversal finished.", transactionData)));

        public void OnReversalFailed(ReversalFailure reversalFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                false,
                NearpaySdkRawDump.Explain("Reverse", reversalFailure)));
    }

    private sealed class ReconcileListener(TaskCompletionSource<NearpayOperationResult<NearpayReconcileResult>> tcs)
        : Java.Lang.Object, IReconcileListener
    {
        public void OnReconcileFinished(ReconciliationReceipt? receipt)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayReconcileResult>(
                true,
                "Reconcile finished.",
                MapReconcileResult("Reconcile finished.", receipt)));

        public void OnReconcileFailed(ReconcileFailure reconcileFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayReconcileResult>(
                false,
                NearpaySdkRawDump.Explain("Reconcile", reconcileFailure)));
    }

    private sealed class CompatibilityListener(TaskCompletionSource<NearpayOperationResult<string>> tcs)
        : Java.Lang.Object, ICompatibilityListener
    {
        public void OnDeviceCompatible()
            => tcs.TrySetResult(new NearpayOperationResult<string>(
                true,
                "Device is compatible with NearPay.",
                "Device is compatible with NearPay."));

        public void OnDeviceIncompatible(CompatibilityFailure compatibilityFailure)
        {
            var explained = NearpaySdkRawDump.Explain("DeviceCompatibility", compatibilityFailure);
            tcs.TrySetResult(new NearpayOperationResult<string>(false, explained, explained));
        }
    }

    private sealed class CheckSessionListener(TaskCompletionSource<NearpayOperationResult<string>> tcs)
        : Java.Lang.Object, ICheckSessionListener
    {
        public void GetSessionInfo(SessionInfo info)
        {
            var raw = NearpaySdkRawDump.Dump(info);
            tcs.TrySetResult(new NearpayOperationResult<string>(true, raw, raw));
        }

        public void OnSessionBusy(string message)
            => tcs.TrySetResult(new NearpayOperationResult<string>(false, message, message));

        public void OnSessionFailed(SessionFailure sessionFailure)
        {
            var explained = NearpaySdkRawDump.Explain("GetUserSession", sessionFailure);
            tcs.TrySetResult(new NearpayOperationResult<string>(false, explained, explained));
        }

        public void OnSessionFree()
            => tcs.TrySetResult(new NearpayOperationResult<string>(true, "Session is free.", "Session is free."));
    }

    private sealed class DismissListener(TaskCompletionSource<NearpayOperationResult> tcs)
        : Java.Lang.Object, IDismissListener
    {
        public void OnDismiss(bool dismissed)
            => tcs.TrySetResult(new NearpayOperationResult(
                dismissed,
                dismissed ? "Dismiss completed." : "Dismiss was not completed."));

        public void OnDismissFailure(DismissFailure dismissFailure)
            => tcs.TrySetResult(new NearpayOperationResult(false, NearpaySdkRawDump.Explain("Dismiss", dismissFailure)));
    }

    private sealed class LogoutListener(TaskCompletionSource<NearpayOperationResult> tcs)
        : Java.Lang.Object, ILogoutListener
    {
        public void OnLogoutCompleted()
            => tcs.TrySetResult(new NearpayOperationResult(true, "Logout completed."));

        public void OnLogoutFailed(LogoutFailure logoutFailure)
            => tcs.TrySetResult(new NearpayOperationResult(false, NearpaySdkRawDump.Explain("Logout", logoutFailure)));
    }

    private sealed class GetReconcileReceiptListener(TaskCompletionSource<NearpayOperationResult<NearpayReconcileResult>> tcs)
        : Java.Lang.Object, IGetReconcileListener
    {
        public void OnSuccess(ReconciliationReceipt? receipt)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayReconcileResult>(
                true,
                "Reconciliation receipt loaded.",
                MapReconcileResult("Reconciliation receipt loaded.", receipt)));

        public void OnFailure(GetDataFailure failure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayReconcileResult>(
                false,
                NearpaySdkRawDump.Explain("GetReconciliationReceipt", failure)));
    }
}
