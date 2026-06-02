using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
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
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string busyMessage = "";



    public ObservableCollection<string> Logs { get; } = new();

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Logs.Insert(0, line);
        StatusMessage = message;
    }

    private async Task RunBusyAsync(string message, Func<CancellationToken, Task> action, CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            BusyMessage = message;
            Log(message);
            await action(ct);
        }
        catch (TaskCanceledException)
        {
            Log("انتهت مهلة العملية أو تم إلغاؤها.");
        }
        catch (Exception ex)
        {
            Log($"خطأ غير متوقع: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = "";
        }
    }

    private static NearpayEnvironment ParseEnv(string value)
        => Enum.TryParse<NearpayEnvironment>(value, out var env) ? env : NearpayEnvironment.Sandbox;

    private static NearpayAuthMode ParseAuthMode(string value)
        => Enum.TryParse<NearpayAuthMode>(value, out var mode) ? mode : NearpayAuthMode.UserEnter;

    [RelayCommand]
    private async Task Initialize(CancellationToken ct)
    {
        await RunBusyAsync("جاري تهيئة NearPay...", async innerCt =>
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

                await _nearpay.InitializeAsync(req, innerCt);
                Log("تم تهيئة NearPay بنجاح");
            }
            catch (Exception ex)
            {
                Log($"فشل التهيئة: {ex.Message}");
            }
        }, ct);
    }

    [RelayCommand]
    private async Task Setup(CancellationToken ct)
    {
        if (!_nearpay.IsInitialized)
        {
            Log("الرجاء الضغط على (تهيئة) أولاً قبل (تسجيل الجهاز)");
            return;
        }

        await RunBusyAsync("جاري تسجيل الجهاز (Setup)...", async innerCt =>
        {
            // Setup قد يفتح واجهة NearPay (تثبيت/تسجيل دخول/اختيار Terminal).
            // لو لم يكتمل خلال فترة طويلة نوقفه لتجنب تجميد الواجهة.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(innerCt);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(2));

            var result = await _nearpay.SetupAsync(timeoutCts.Token);
            Log(result.IsSuccess ? $"Setup OK: {result.Message}" : $"Setup Failed: {result.Message}");

            if (!result.IsSuccess)
            {
                // إرشادات مختصرة لأكثر أسباب الفشل شيوعاً
                Log("تأكد من: (1) تسجيل Package Name في Dashboard (2) إنشاء Terminal وأخذ Tid (3) Invite user ثم قبول الدعوة (4) تثبيت Payment Plugin عند الطلب.");
            }
        }, ct);
    }

    [RelayCommand]
    private async Task ShowHelp(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var text =
            "التهيئة = إنشاء كائن NearPay داخل التطبيق.\n" +
            "تسجيل الجهاز (Setup) = يثبت/يفعّل Payment Plugin ويسجّل دخول المستخدم/يربطه بالـ Terminal.\n\n" +
            "المطلوب قبل Setup:\n" +
            "1) Dashboard (Sandbox): Apps → Add App وسجّل Package Name.\n" +
            "2) Dashboard: Terminals → Create terminal وخذ Tid.\n" +
            "3) Terminals → Access → Invite user (بالإيميل/الموبايل) ثم تأكد أنه قبل الدعوة.\n" +
            "4) في التطبيق اختر Auth Mode:\n" +
            "- UserEnter: الأسهل للتجربة.\n" +
            "- Email/Mobile: لازم نفس المستخدم يكون Invited.\n" +
            "- JWT: تولّده من private-key.pem (لا يوضع داخل التطبيق) ثم تلصق الـ token داخل Auth Value.\n\n" +
            "بعدها: اضغط (تهيئة) ثم (تسجيل الجهاز) ثم Purchase.";

        await MainThread.InvokeOnMainThreadAsync(() =>
            Application.Current!.MainPage!.DisplayAlert("مساعدة سريعة", text, "تمام"));
    }

    [RelayCommand]
    private async Task Purchase(CancellationToken ct)
    {
        await RunBusyAsync("جاري تنفيذ Purchase...", async innerCt =>
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

            var result = await _nearpay.PurchaseAsync(req, innerCt);
            Log(result.IsSuccess ? $"Purchase Approved: {result.Data?.Summary}" : $"Purchase Failed: {result.Message}");
        }, ct);
    }

    [RelayCommand]
    private async Task Refund(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(TransactionUuid))
        {
            Log("الرجاء إدخال Transaction UUID");
            return;
        }

        await RunBusyAsync("جاري تنفيذ Refund...", async innerCt =>
        {
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

            var result = await _nearpay.RefundAsync(req, innerCt);
            Log(result.IsSuccess ? $"Refund Approved: {result.Data?.Summary}" : $"Refund Failed: {result.Message}");
        }, ct);
    }

    [RelayCommand]
    private async Task Reverse(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(TransactionUuid))
        {
            Log("الرجاء إدخال Transaction UUID");
            return;
        }

        await RunBusyAsync("جاري تنفيذ Reverse...", async innerCt =>
        {
            var req = new NearpayReverseRequest(
                TransactionUuid.Trim(),
                EnableReceiptUi,
                FinishTimeoutSeconds,
                EnableUiDismiss
            );

            var result = await _nearpay.ReverseAsync(req, innerCt);
            Log(result.IsSuccess ? $"Reverse OK: {result.Data?.Summary}" : $"Reverse Failed: {result.Message}");
        }, ct);
    }

    [RelayCommand]
    private async Task Reconcile(CancellationToken ct)
    {
        await RunBusyAsync("جاري تنفيذ Reconcile...", async innerCt =>
        {
            var req = new NearpayReconcileRequest(
                EnableReceiptUi,
                FinishTimeoutSeconds,
                ReconcileId: Guid.NewGuid(),
                AdminPin,
                EnableUiDismiss
            );

            var result = await _nearpay.ReconcileAsync(req, innerCt);
            Log(result.IsSuccess ? $"Reconcile OK: {result.Data?.Summary}" : $"Reconcile Failed: {result.Message}");
        }, ct);
    }

    [RelayCommand]
    private void ClearLog()
    {
        Logs.Clear();
        StatusMessage = "تم مسح السجل";
    }
}
