using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.ApplicationModel.Communication;
using NearpayPosMauiDemo.App.Services;
using NearpayPosMauiDemo.Core.Abstractions;
using NearpayPosMauiDemo.Core.Models;

namespace NearpayPosMauiDemo.App.Presentation.Main;

public partial class MainPageViewModel : ObservableObject
{
    private readonly INearpayService _nearpay;
    private readonly INearpaySettingsStore _settingsStore;

    public MainPageViewModel(INearpayService nearpay, INearpaySettingsStore settingsStore)
    {
        _nearpay = nearpay;
        _settingsStore = settingsStore;

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

        _ = LoadSavedSettingsAsync();
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
    [ObservableProperty] private string logText = "";



    public ObservableCollection<string> Logs { get; } = new();

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Logs.Add(line);
        StatusMessage = message;
        LogText = string.IsNullOrEmpty(LogText) ? line : (LogText + Environment.NewLine + line);
    }

    private async Task RunBusyAsync(string message, Func<CancellationToken, Task> action, CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            BusyMessage = message;
            await action(ct);
        }
        catch (TaskCanceledException)
        {
            Log("TaskCanceledException");
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
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

    private async Task LoadSavedSettingsAsync()
    {
        try
        {
            var s = await _settingsStore.LoadAsync();
            SelectedEnvironment = s.Environment;
            SelectedAuthMode = s.AuthMode;
            AuthValue = s.AuthValue ?? "";
            Tid = s.Tid ?? "";
            Locale = string.IsNullOrWhiteSpace(s.Locale) ? "ar-SA" : s.Locale;
        }
        catch
        {
            // ignore
        }
    }

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
                Log("InitializeAsync: OK");
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }, ct);
    }

    [RelayCommand]
    private async Task Setup(CancellationToken ct)
    {
        if (!_nearpay.IsInitialized)
        {
            Log("NotInitialized");
            return;
        }

        await RunBusyAsync("جاري تسجيل الجهاز (Setup)...", async innerCt =>
        {
            // Setup قد يفتح واجهة NearPay (تثبيت/تسجيل دخول/اختيار Terminal).
            // لو لم يكتمل خلال فترة طويلة نوقفه لتجنب تجميد الواجهة.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(innerCt);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(2));

            var result = await _nearpay.SetupAsync(timeoutCts.Token);
            Log(result.Message);
        }, ct);
    }

    [RelayCommand]
    private async Task ShowHelp(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var text =
            "التهيئة = إنشاء كائن NearPay داخل التطبيق.\n" +
            "تسجيل الجهاز (Setup) = ينفّذ عملية Setup داخل NearPay SDK ويُكمل متطلبات تشغيل الدفع.\n\n" +
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
    private async Task PrepareDevice(CancellationToken ct)
    {
        // هدف الزر: تسهيل منح الصلاحيات المطلوبة قبل Setup بدل أن يفشل المستخدم بدون سبب واضح.
        await RunBusyAsync("جاري تجهيز الجهاز (صلاحيات/إعدادات)...", async innerCt =>
        {
            // 1) Location permission (NearPay status checks غالباً تتطلبه)
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            // 2) افتح إعدادات التطبيق إذا لم تُمنح الصلاحية (أو يحتاج المستخدم تفعيل Location Service)
            if (status != PermissionStatus.Granted)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current!.MainPage!.DisplayAlert(
                        "صلاحيات مطلوبة",
                        "الرجاء منح صلاحية الموقع للتطبيق، وكذلك تفعيل خدمة الموقع (Location) من إعدادات الجهاز.",
                        "فتح الإعدادات"));
                AppInfo.ShowSettingsUI();
            }
        }, ct);
    }

    [RelayCommand]
    private async Task DeviceCompatibility(CancellationToken ct)
    {
        if (!_nearpay.IsInitialized)
        {
            Log("NotInitialized");
            return;
        }

        var res = await _nearpay.DeviceCompatibilityAsync(ct);
        Log(res.Message);
    }

    [RelayCommand]
    private async Task GetUserSession(CancellationToken ct)
    {
        if (!_nearpay.IsInitialized)
        {
            Log("NotInitialized");
            return;
        }

        var res = await _nearpay.GetUserSessionAsync(ct);
        Log(res.Message);
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
            Log(result.IsSuccess ? (result.Data?.Raw ?? result.Data?.Summary ?? result.Message) : result.Message);
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
            Log(result.IsSuccess ? (result.Data?.Raw ?? result.Data?.Summary ?? result.Message) : result.Message);
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
            Log(result.IsSuccess ? (result.Data?.Raw ?? result.Data?.Summary ?? result.Message) : result.Message);
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
            Log(result.IsSuccess ? (result.Data?.Raw ?? result.Data?.Summary ?? result.Message) : result.Message);
        }, ct);
    }

    [RelayCommand]
    private void ClearLog()
    {
        Logs.Clear();
        LogText = "";
        StatusMessage = "Cleared";
    }

    [RelayCommand]
    private async Task OpenSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await Shell.Current.GoToAsync("//NearpaySettingsPage");
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
        }
    }

    [RelayCommand]
    private async Task CopyLog(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Clipboard.Default.SetTextAsync(LogText ?? string.Empty);
    }

    [RelayCommand]
    private async Task RunOneButtonPurchase(CancellationToken ct)
    {
        // تجربة برمجية واحدة: Initialize -> deviceCompatibility -> getUserSession -> Setup -> Purchase
        // تعتمد على الإعدادات المحفوظة محلياً (وليست حقول الواجهة).
        await RunBusyAsync("Running...", async innerCt =>
        {
            var s = await _settingsStore.LoadAsync();

            Log("STEP: Initialize");
            var initReq = new NearpayInitializationRequest(
                Environment: ParseEnv(s.Environment),
                AuthMode: ParseAuthMode(s.AuthMode),
                AuthValue: s.AuthValue ?? string.Empty,
                Tid: s.Tid ?? string.Empty,
                Locale: s.Locale ?? "ar-SA");
            await _nearpay.InitializeAsync(initReq, innerCt);
            Log("InitializeAsync: OK");

            Log("STEP: Setup");
            var setup = await _nearpay.SetupAsync(innerCt);
            Log(setup.Message);
            if (!setup.IsSuccess) return;

            // اختياري: فحوصات SDK بعد نجاح Setup (لا يجب أن تمنع تجربة الشراء)
            Log("STEP: DeviceCompatibility");
            var comp = await _nearpay.DeviceCompatibilityAsync(innerCt);
            Log(comp.Message);

            Log("STEP: GetUserSession");
            var sess = await _nearpay.GetUserSessionAsync(innerCt);
            Log(sess.Message);

            Log("STEP: Purchase");
            var purchaseReq = new NearpayPurchaseRequest(
                AmountMinor: 100, // 1.00
                CustomerReferenceNumber: "",
                EnableReceiptUi: true,
                EnableReversal: true,
                FinishTimeoutSeconds: 60,
                RequestId: Guid.NewGuid(),
                EnableUiDismiss: true);

            var purchase = await _nearpay.PurchaseAsync(purchaseReq, innerCt);
            Log(purchase.IsSuccess
                ? (purchase.Data?.Raw ?? purchase.Data?.Summary ?? purchase.Message)
                : purchase.Message);
        }, ct);
    }

    [RelayCommand]
    private async Task DownloadPaymentPluginSandbox(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // رابط رسمي من NearPay (Payment Plugin Changelog)
        await Browser.Default.OpenAsync(
            "https://firebasestorage.googleapis.com/v0/b/nearpayio/o/payments-plugins%2Fsnadbox%2Fpayment-plugin-sandboxNearpayStore-163-protected.apk?alt=media&token=c1da7097-8a34-4b0e-aad4-ec3fb13b8b17",
            BrowserLaunchMode.External);
    }

    [RelayCommand]
    private async Task DownloadPaymentPluginProduction(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // رابط رسمي من NearPay (Payment Plugin Changelog)
        await Browser.Default.OpenAsync(
            "https://firebasestorage.googleapis.com/v0/b/nearpayio/o/payments-plugins%2Fproduction%2Fpayment-plugin-productionNearpayStore-163-protected.apk?alt=media&token=76303e0d-28f4-44f8-82dc-04363b8313b3",
            BrowserLaunchMode.External);
    }
}
