using NearpayPosMauiDemo.Core.Models;

namespace NearpayPosMauiDemo.Core.Abstractions;

public interface INearpayService
{
    bool IsInitialized { get; }

    Task InitializeAsync(NearpayInitializationRequest request, CancellationToken ct = default);

    Task<NearpayOperationResult> SetupAsync(CancellationToken ct = default);

    Task<NearpayOperationResult<string>> DeviceCompatibilityAsync(CancellationToken ct = default);

    Task<NearpayOperationResult<string>> GetUserSessionAsync(CancellationToken ct = default);

    Task<NearpayOperationResult<NearpayTransactionResult>> PurchaseAsync(
        NearpayPurchaseRequest request,
        CancellationToken ct = default);

    Task<NearpayOperationResult<NearpayTransactionResult>> RefundAsync(
        NearpayRefundRequest request,
        CancellationToken ct = default);

    Task<NearpayOperationResult<NearpayTransactionResult>> ReverseAsync(
        NearpayReverseRequest request,
        CancellationToken ct = default);

    Task<NearpayOperationResult<NearpayReconcileResult>> ReconcileAsync(
        NearpayReconcileRequest request,
        CancellationToken ct = default);
}
