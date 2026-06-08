using Android.App;
using NearpayPosMauiDemo.Core.Abstractions;
using NearpayPosMauiDemo.Core.Models;

using IO.Nearpay.Sdk;
using IO.Nearpay.Sdk.Data.Models;
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
            .Locale(JLocale.Default!);

        _nearPay = builder.Build();
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

        var tcs = new TaskCompletionSource<NearpayOperationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try { nearPay.Setup(new SetupListener(tcs)); }
            catch (Exception ex) { tcs.TrySetResult(new NearpayOperationResult(false, NearpaySdkRawDump.Explain("Setup", ex))); }
        });
        return await tcs.Task.ConfigureAwait(false);
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
            return new NearpayOperationResult(
                false,
                $"NearPay: Payment Plugin غير مثبت ({pluginPackage}).");
        }
    }

    public async Task<NearpayOperationResult<string>> DeviceCompatibilityAsync(CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try { nearPay.DeviceCompatibility(new CompatibilityListener(tcs)); }
            catch (Exception ex) { tcs.TrySetResult(new NearpayOperationResult<string>(false, NearpaySdkRawDump.Explain("DeviceCompatibility", ex), NearpaySdkRawDump.Dump(ex))); }
        });
        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<string>> GetUserSessionAsync(CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try { nearPay.GetUserSession(new CheckSessionListener(tcs)); }
            catch (Exception ex) { tcs.TrySetResult(new NearpayOperationResult<string>(false, NearpaySdkRawDump.Explain("GetUserSession", ex), NearpaySdkRawDump.Dump(ex))); }
        });
        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> PurchaseAsync(NearpayPurchaseRequest request, CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                nearPay.Purchase(
                    amount: request.AmountMinor,
                    customerReferenceNumber: request.CustomerReferenceNumber,
                    enableReceiptUi: request.EnableReceiptUi,
                    enableReversal: request.EnableReversal,
                    finishTimeOut: request.FinishTimeoutSeconds,
                    requestId: request.RequestId is null ? null : UUID.FromString(request.RequestId.Value.ToString()),
                    enableUiDismiss: request.EnableUiDismiss,
                    listener: new PurchaseListener(tcs));
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                    false,
                    NearpaySdkRawDump.Explain("Purchase", ex)));
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> RefundAsync(NearpayRefundRequest request, CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                    false,
                    NearpaySdkRawDump.Explain("Refund", ex)));
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayTransactionResult>> ReverseAsync(NearpayReverseRequest request, CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                nearPay.Reverse(
                    transactionUuid: request.TransactionUuid,
                    enableReceiptUi: request.EnableReceiptUi,
                    finishTimeOut: request.FinishTimeoutSeconds,
                    enableUiDismiss: request.EnableUiDismiss,
                    listener: new ReversalListener(tcs));
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                    false,
                    NearpaySdkRawDump.Explain("Reverse", ex)));
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<NearpayOperationResult<NearpayReconcileResult>> ReconcileAsync(NearpayReconcileRequest request, CancellationToken ct = default)
    {
        var nearPay = EnsureInitialized();

        var tcs = new TaskCompletionSource<NearpayOperationResult<NearpayReconcileResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                nearPay.Reconcile(
                    reconcileId: request.ReconcileId is null ? null : UUID.FromString(request.ReconcileId.Value.ToString()),
                    enableReceiptUi: request.EnableReceiptUi,
                    adminPin: request.AdminPin,
                    finishTimeOut: request.FinishTimeoutSeconds,
                    enableUiDismiss: request.EnableUiDismiss,
                    listener: new ReconcileListener(tcs));
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(new NearpayOperationResult<NearpayReconcileResult>(
                    false,
                    NearpaySdkRawDump.Explain("Reconcile", ex)));
            }
        });

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

    private static void ValidateRequest(NearpayInitializationRequest request)
    {
        var v = request.AuthValue?.Trim();

        if (request.AuthMode is NearpayAuthMode.Jwt or NearpayAuthMode.Email or NearpayAuthMode.Mobile)
        {
            if (string.IsNullOrWhiteSpace(v))
                throw new InvalidOperationException("طريقة الدخول المختارة تتطلب Auth Value. اكتب JWT/Email/Mobile أو اختر UserEnter.");
        }
    }

    // -----------------
    // Listener adapters
    // -----------------

    private sealed class SetupListener(TaskCompletionSource<NearpayOperationResult> tcs)
        : Java.Lang.Object, ISetupListener
    {
        public void OnSetupCompleted()
            => tcs.TrySetResult(new NearpayOperationResult(true, "OnSetupCompleted"));

        public void OnSetupFailed(SetupFailure setupFailure)
            => tcs.TrySetResult(new NearpayOperationResult(false, NearpaySdkRawDump.Explain("Setup", setupFailure)));
    }

    private sealed class PurchaseListener(TaskCompletionSource<NearpayOperationResult<NearpayTransactionResult>> tcs)
        : Java.Lang.Object, IPurchaseListener
    {
        public void OnPurchaseApproved(TransactionData transactionData)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                true,
                "OnPurchaseApproved",
                new NearpayTransactionResult(transactionData.ToString() ?? "OnPurchaseApproved", transactionData.ToString())));

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
                "OnRefundApproved",
                new NearpayTransactionResult(transactionData.ToString() ?? "OnRefundApproved", transactionData.ToString())));

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
                "OnReversalFinished",
                new NearpayTransactionResult(transactionData.ToString() ?? "OnReversalFinished", transactionData.ToString())));

        public void OnReversalFailed(ReversalFailure reversalFailure)
            => tcs.TrySetResult(new NearpayOperationResult<NearpayTransactionResult>(
                false,
                NearpaySdkRawDump.Explain("Reverse", reversalFailure)));
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
                NearpaySdkRawDump.Explain("Reconcile", reconcileFailure)));
    }

    private sealed class CompatibilityListener(TaskCompletionSource<NearpayOperationResult<string>> tcs)
        : Java.Lang.Object, ICompatibilityListener
    {
        public void OnDeviceCompatible()
            => tcs.TrySetResult(new NearpayOperationResult<string>(true, "OnDeviceCompatible", "OnDeviceCompatible"));

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
            => tcs.TrySetResult(new NearpayOperationResult<string>(true, "OnSessionFree", "OnSessionFree"));
    }
}
