using Android.App;
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

        var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;

        var builder = new NearPay.Builder()
            .Context(context)
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
            => tcs.TrySetResult(new NearpayOperationResult(true, "Completed"));

        public void OnSetupFailed(SetupFailure setupFailure)
            => tcs.TrySetResult(new NearpayOperationResult(false, setupFailure.ToString() ?? "Setup failed"));
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
}
