namespace NearpayPosMauiDemo.Core.Models;

public enum NearpayEnvironment
{
    Sandbox,
    Production,
    Staging,
    Testing
}

public enum NearpayAuthMode
{
    UserEnter,
    Mobile,
    Email,
    Jwt,
    DeviceLinking
}

public sealed record NearpayInitializationRequest(
    NearpayEnvironment Environment,
    NearpayAuthMode AuthMode,
    string? AuthValue,
    string? Tid = null,
    string? Locale = null // e.g. "ar-SA" or "en-US"
);

public sealed record NearpayPurchaseRequest(
    long AmountMinor, // 1455 => 14.55
    string? CustomerReferenceNumber,
    bool EnableReceiptUi,
    bool EnableReversal,
    long FinishTimeoutSeconds,
    Guid? RequestId,
    bool EnableUiDismiss
);

public sealed record NearpayRefundRequest(
    long AmountMinor,
    string TransactionUuid,
    string? CustomerReferenceNumber,
    bool EnableReceiptUi,
    bool EnableReversal,
    bool EnableEditableRefundAmountUi,
    long FinishTimeoutSeconds,
    Guid? RequestId,
    string? AdminPin,
    bool EnableUiDismiss
);

public sealed record NearpayReverseRequest(
    string TransactionUuid,
    bool EnableReceiptUi,
    long FinishTimeoutSeconds,
    bool EnableUiDismiss
);

public sealed record NearpayReconcileRequest(
    bool EnableReceiptUi,
    long FinishTimeoutSeconds,
    Guid? ReconcileId,
    string? AdminPin,
    bool EnableUiDismiss
);

public sealed record NearpayTransactionResult(
    string Summary,
    string? Raw = null
);

public sealed record NearpayReconcileResult(
    string Summary,
    string? Raw = null
);

public sealed record NearpayOperationResult(
    bool IsSuccess,
    string Message,
    Exception? Exception = null
);

public sealed record NearpayOperationResult<T>(
    bool IsSuccess,
    string Message,
    T? Data = default,
    Exception? Exception = null
);

