using NearpayPosMauiDemo.Core.Abstractions;
using NearpayPosMauiDemo.Core.Models;

namespace NearpayPosMauiDemo.App.Services;

public sealed class NearpayServiceStub : INearpayService
{
    public bool IsInitialized => false;

    public Task InitializeAsync(NearpayInitializationRequest request, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("NearPay SDK مدعوم على Android فقط.");

    public Task<NearpayOperationResult> SetupAsync(CancellationToken ct = default)
        => Task.FromResult(new NearpayOperationResult(false, "غير مدعوم على هذا النظام"));

    public Task<NearpayOperationResult<string>> DeviceCompatibilityAsync(CancellationToken ct = default)
        => Task.FromResult(new NearpayOperationResult<string>(false, "غير مدعوم على هذا النظام"));

    public Task<NearpayOperationResult<string>> GetUserSessionAsync(CancellationToken ct = default)
        => Task.FromResult(new NearpayOperationResult<string>(false, "غير مدعوم على هذا النظام"));

    public Task<NearpayOperationResult<NearpayTransactionResult>> PurchaseAsync(NearpayPurchaseRequest request, CancellationToken ct = default)
        => Task.FromResult(new NearpayOperationResult<NearpayTransactionResult>(false, "غير مدعوم على هذا النظام"));

    public Task<NearpayOperationResult<NearpayTransactionResult>> RefundAsync(NearpayRefundRequest request, CancellationToken ct = default)
        => Task.FromResult(new NearpayOperationResult<NearpayTransactionResult>(false, "غير مدعوم على هذا النظام"));

    public Task<NearpayOperationResult<NearpayTransactionResult>> ReverseAsync(NearpayReverseRequest request, CancellationToken ct = default)
        => Task.FromResult(new NearpayOperationResult<NearpayTransactionResult>(false, "غير مدعوم على هذا النظام"));

    public Task<NearpayOperationResult<NearpayReconcileResult>> ReconcileAsync(NearpayReconcileRequest request, CancellationToken ct = default)
        => Task.FromResult(new NearpayOperationResult<NearpayReconcileResult>(false, "غير مدعوم على هذا النظام"));
}
