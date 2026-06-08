using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NearpayPosMauiDemo.App.Services;
using NearpayPosMauiDemo.Core.Abstractions;
using NearpayPosMauiDemo.Core.Models;
#if ANDROID
using Android.OS;
using IO.Nearpay.Sdk_internal.Data;
#endif

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
        EnableUiDismiss = true;

        _ = LoadSavedSettingsAsync();
    }

    public string[] EnvironmentOptions { get; }
    public string[] AuthModeOptions { get; }

    [ObservableProperty] private string selectedEnvironment;
    [ObservableProperty] private string selectedAuthMode;
    [ObservableProperty] private string? authValue;

    [ObservableProperty] private long amountMinor;
    [ObservableProperty] private string? customerReferenceNumber;
    [ObservableProperty] private long finishTimeoutSeconds;

    [ObservableProperty] private bool enableReceiptUi;
    [ObservableProperty] private bool enableReversal;
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
        LogText = string.IsNullOrEmpty(LogText) ? line : (LogText + System.Environment.NewLine + line);
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
            SelectedEnvironment = string.IsNullOrWhiteSpace(s.Environment) ? NearpayEnvironment.Sandbox.ToString() : s.Environment;
            SelectedAuthMode = string.IsNullOrWhiteSpace(s.AuthMode) ? NearpayAuthMode.UserEnter.ToString() : s.AuthMode;
            AuthValue = s.AuthValue ?? "";
        }
        catch
        {
            // ignore
        }
    }

    [RelayCommand]
    private async Task RunOneButtonPurchase(CancellationToken ct)
    {
        await RunBusyAsync("Running: Initialize → Setup → Purchase ...", async innerCt =>
        {
            var authMode = ParseAuthMode(SelectedAuthMode);
            var authValue = AuthValue?.Trim();
            if (authMode is NearpayAuthMode.Jwt or NearpayAuthMode.Email or NearpayAuthMode.Mobile)
            {
                if (string.IsNullOrWhiteSpace(authValue))
                {
                    Log("AuthValue مطلوب لهذه الطريقة. اكتب JWT/Email/Mobile أو اختر UserEnter.");
                    return;
                }
            }

            Log("STEP: Initialize");
            var initReq = new NearpayInitializationRequest(
                Environment: ParseEnv(SelectedEnvironment),
                AuthMode: authMode,
                AuthValue: authValue ?? string.Empty,
                Tid: "",
                Locale: null);
            await _nearpay.InitializeAsync(initReq, innerCt);
            Log("InitializeAsync: OK");

#if ANDROID
            try
            {
                var env = ParseEnv(SelectedEnvironment);
                var pluginPackage = env == NearpayEnvironment.Production
                    ? DataEnvironments.Production!.PluginPackageName
                    : DataEnvironments.Sandbox!.PluginPackageName;

                Log($"Android SDK: {(int)Build.VERSION.SdkInt}");
                Log($"NearPay plugin package: {pluginPackage}");
            }
            catch (Exception ex)
            {
                Log($"DIAG: {ex.GetType().Name}: {ex.Message}");
            }
#endif

            Log("STEP: Setup");
            using var setupTimeout = CancellationTokenSource.CreateLinkedTokenSource(innerCt);
            setupTimeout.CancelAfter(TimeSpan.FromMinutes(10));
            var setup = await _nearpay.SetupAsync(setupTimeout.Token);
            Log(setup.Message);
            if (!setup.IsSuccess) return;

            Log("STEP: Purchase");
            var purchaseReq = new NearpayPurchaseRequest(
                AmountMinor: AmountMinor,
                CustomerReferenceNumber: CustomerReferenceNumber,
                EnableReceiptUi: EnableReceiptUi,
                EnableReversal: EnableReversal,
                FinishTimeoutSeconds: FinishTimeoutSeconds,
                RequestId: Guid.NewGuid(),
                EnableUiDismiss: EnableUiDismiss);

            var purchase = await _nearpay.PurchaseAsync(purchaseReq, innerCt);
            Log(purchase.IsSuccess
                ? (purchase.Data?.Raw ?? purchase.Data?.Summary ?? purchase.Message)
                : purchase.Message);
        }, ct);
    }

}
