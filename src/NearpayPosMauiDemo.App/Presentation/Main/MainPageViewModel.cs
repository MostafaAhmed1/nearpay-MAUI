using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NearpayPosMauiDemo.Core.Abstractions;
using NearpayPosMauiDemo.Core.Models;

namespace NearpayPosMauiDemo.App.Presentation.Main;

public partial class MainPageViewModel : ObservableObject
{
    private readonly INearpayService _nearpay;

    public MainPageViewModel(INearpayService nearpay)
    {
        _nearpay = nearpay;

        EnvironmentOptions = Enum.GetNames(typeof(NearpayEnvironment));
        AuthModeOptions = Enum.GetNames(typeof(NearpayAuthMode));

        SelectedEnvironment = NearpayEnvironment.Sandbox.ToString();
        SelectedAuthMode = NearpayAuthMode.UserEnter.ToString();

        AmountMinor = 100; // 1.00
        FinishTimeoutSeconds = 10;
        EnableReceiptUi = true;
        EnableReversal = true;
        EnableEditableRefundAmountUi = true;
        EnableUiDismiss = true;
    }

    public string[] EnvironmentOptions { get; }
    public string[] AuthModeOptions { get; }

    [ObservableProperty] private string selectedEnvironment;
    [ObservableProperty] private string selectedAuthMode;
    [ObservableProperty] private string? authValue;
    [ObservableProperty] private string? tid;
    [ObservableProperty] private string? locale = "ar-SA";

    [ObservableProperty] private long amountMinor;
    [ObservableProperty] private string? customerReferenceNumber;
    [ObservableProperty] private string? transactionUuid;
    [ObservableProperty] private string? adminPin;
    [ObservableProperty] private long finishTimeoutSeconds;

    [ObservableProperty] private bool enableReceiptUi;
    [ObservableProperty] private bool enableReversal;
    [ObservableProperty] private bool enableEditableRefundAmountUi;
    [ObservableProperty] private bool enableUiDismiss;

    [ObservableProperty] private string statusMessage = "جاهز";

    public ObservableCollection<string> Logs { get; } = new();

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Logs.Insert(0, line);
        StatusMessage = message;
    }

    private static NearpayEnvironment ParseEnv(string value)
        => Enum.TryParse<NearpayEnvironment>(value, out var env) ? env : NearpayEnvironment.Sandbox;

    private static NearpayAuthMode ParseAuthMode(string value)
        => Enum.TryParse<NearpayAuthMode>(value, out var mode) ? mode : NearpayAuthMode.UserEnter;

    [RelayCommand]
    private async Task Initialize(CancellationToken ct)
    {
        try
        {
            var req = new NearpayInitializationRequest(
                Environment: ParseEnv(SelectedEnvironment),
                AuthMode: ParseAuthMode(SelectedAuthMode),
                AuthValue: AuthValue,
                Tid: Tid,
                Locale: Locale
            );

            await _nearpay.InitializeAsync(req, ct);
            Log("تم تهيئة NearPay بنجاح");
        }
        catch (Exception ex)
        {
            Log($"فشل التهيئة: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Setup(CancellationToken ct)
    {
        var result = await _nearpay.SetupAsync(ct);
        Log(result.IsSuccess ? $"Setup OK: {result.Message}" : $"Setup Failed: {result.Message}");
    }

    [RelayCommand]
    private async Task Purchase(CancellationToken ct)
    {
        var req = new NearpayPurchaseRequest(
            AmountMinor,
            CustomerReferenceNumber,
            EnableReceiptUi,
            EnableReversal,
            FinishTimeoutSeconds,
            RequestId: Guid.NewGuid(),
            EnableUiDismiss
        );

        var result = await _nearpay.PurchaseAsync(req, ct);
        Log(result.IsSuccess ? $"Purchase Approved: {result.Data?.Summary}" : $"Purchase Failed: {result.Message}");
    }

    [RelayCommand]
    private async Task Refund(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(TransactionUuid))
        {
            Log("الرجاء إدخال Transaction UUID");
            return;
        }

        var req = new NearpayRefundRequest(
            AmountMinor,
            TransactionUuid.Trim(),
            CustomerReferenceNumber,
            EnableReceiptUi,
            EnableReversal,
            EnableEditableRefundAmountUi,
            FinishTimeoutSeconds,
            RequestId: Guid.NewGuid(),
            AdminPin,
            EnableUiDismiss
        );

        var result = await _nearpay.RefundAsync(req, ct);
        Log(result.IsSuccess ? $"Refund Approved: {result.Data?.Summary}" : $"Refund Failed: {result.Message}");
    }

    [RelayCommand]
    private async Task Reverse(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(TransactionUuid))
        {
            Log("الرجاء إدخال Transaction UUID");
            return;
        }

        var req = new NearpayReverseRequest(
            TransactionUuid.Trim(),
            EnableReceiptUi,
            FinishTimeoutSeconds,
            EnableUiDismiss
        );

        var result = await _nearpay.ReverseAsync(req, ct);
        Log(result.IsSuccess ? $"Reverse OK: {result.Data?.Summary}" : $"Reverse Failed: {result.Message}");
    }

    [RelayCommand]
    private async Task Reconcile(CancellationToken ct)
    {
        var req = new NearpayReconcileRequest(
            EnableReceiptUi,
            FinishTimeoutSeconds,
            ReconcileId: Guid.NewGuid(),
            AdminPin,
            EnableUiDismiss
        );

        var result = await _nearpay.ReconcileAsync(req, ct);
        Log(result.IsSuccess ? $"Reconcile OK: {result.Data?.Summary}" : $"Reconcile Failed: {result.Message}");
    }

    [RelayCommand]
    private void ClearLog()
    {
        Logs.Clear();
        StatusMessage = "تم مسح السجل";
    }
}
