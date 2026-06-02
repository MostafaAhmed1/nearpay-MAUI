using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using NearpayPosMauiDemo.App.Services;
using NearpayPosMauiDemo.Core.Models;

namespace NearpayPosMauiDemo.App.Presentation.Api;

public partial class NearpayApiViewModel : ObservableObject
{
    private readonly INearpayApiClient _api;
    private readonly INearpaySettingsStore _store;

    public NearpayApiViewModel(INearpayApiClient api, INearpaySettingsStore store)
    {
        _api = api;
        _store = store;

        EnvironmentOptions = Enum.GetNames(typeof(NearpayEnvironment));
        SelectedEnvironment = nameof(NearpayEnvironment.Sandbox);

        _ = LoadAsync();
    }

    public string[] EnvironmentOptions { get; }

    [ObservableProperty] private string selectedEnvironment;
    [ObservableProperty] private string? apiKey;
    [ObservableProperty] private string outputText = "";
    [ObservableProperty] private bool isBusy;

    private async Task LoadAsync()
    {
        try
        {
            var s = await _store.LoadAsync();
            SelectedEnvironment = s.Environment;
            ApiKey = s.ApiKey;
        }
        catch
        {
            // ignore
        }
    }

    private static string MerchantBaseUrl(NearpayEnvironment env)
        => env == NearpayEnvironment.Production
            ? "https://api.nearpay.io/v1/merchants-sdk"
            : "https://sandbox-api.nearpay.io/v1/merchants-sdk";

    private static string PosBaseUrl(NearpayEnvironment env)
        => env == NearpayEnvironment.Production
            ? "https://api.nearpay.io/v1/clients-sdk/pos"
            : "https://sandbox-api.nearpay.io/v1/clients-sdk/pos";

    private NearpayEnvironment Env()
        => Enum.TryParse<NearpayEnvironment>(SelectedEnvironment, out var e) ? e : NearpayEnvironment.Sandbox;

    private void Append(string text)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {text}";
        OutputText = string.IsNullOrEmpty(OutputText) ? line : (OutputText + Environment.NewLine + line);
    }

    private async Task CallAsync(string baseUrl, string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            Append("Missing API Key");
            return;
        }

        IsBusy = true;
        try
        {
            Append($"GET {baseUrl}{path}");
            var (status, body) = await _api.GetAsync(baseUrl, path, ApiKey.Trim(), ct);
            Append($"HTTP {status}");
            Append(body);
        }
        catch (Exception ex)
        {
            Append(ex.ToString());
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task ListTerminals(CancellationToken ct)
        => CallAsync(MerchantBaseUrl(Env()), "/terminals", ct);

    [RelayCommand]
    private Task ListTransactions(CancellationToken ct)
        => CallAsync(MerchantBaseUrl(Env()), "/transactions", ct);

    [RelayCommand]
    private Task ListIntents(CancellationToken ct)
        => CallAsync(MerchantBaseUrl(Env()), "/intents", ct);

    [RelayCommand]
    private Task ListPosMessages(CancellationToken ct)
        => CallAsync(PosBaseUrl(Env()), "/messages", ct);

    [RelayCommand]
    private async Task CopyOutput(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Clipboard.Default.SetTextAsync(OutputText ?? string.Empty);
    }

    [RelayCommand]
    private void Clear()
    {
        OutputText = "";
    }
}

