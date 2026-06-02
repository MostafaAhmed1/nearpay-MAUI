using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NearpayPosMauiDemo.App.Services;
using NearpayPosMauiDemo.Core.Models;

namespace NearpayPosMauiDemo.App.Presentation.Settings;

public partial class NearpaySettingsViewModel : ObservableObject
{
    private readonly INearpaySettingsStore _store;

    public NearpaySettingsViewModel(INearpaySettingsStore store)
    {
        _store = store;
        EnvironmentOptions = Enum.GetNames(typeof(NearpayEnvironment));
        AuthModeOptions = Enum.GetNames(typeof(NearpayAuthMode));

        _ = LoadAsync();
    }

    public string[] EnvironmentOptions { get; }
    public string[] AuthModeOptions { get; }

    [ObservableProperty] private string selectedEnvironment = nameof(NearpayEnvironment.Sandbox);
    [ObservableProperty] private string selectedAuthMode = nameof(NearpayAuthMode.UserEnter);
    [ObservableProperty] private string? authValue;
    [ObservableProperty] private string? tid;
    [ObservableProperty] private string? locale = "ar-SA";
    [ObservableProperty] private string? apiKey;

    [ObservableProperty] private bool isBusy;

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var s = await _store.LoadAsync();
            SelectedEnvironment = s.Environment;
            SelectedAuthMode = s.AuthMode;
            AuthValue = s.AuthValue;
            Tid = s.Tid;
            Locale = s.Locale;
            ApiKey = s.ApiKey;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        IsBusy = true;
        try
        {
            var s = new NearpayLocalSettings(
                SelectedEnvironment,
                SelectedAuthMode,
                string.IsNullOrWhiteSpace(AuthValue) ? null : AuthValue.Trim(),
                string.IsNullOrWhiteSpace(Tid) ? null : Tid.Trim(),
                string.IsNullOrWhiteSpace(Locale) ? null : Locale.Trim(),
                string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey.Trim());

            await _store.SaveAsync(s);
            await Shell.Current.DisplayAlert("تم", "تم حفظ الإعدادات محلياً على الجهاز.", "حسناً");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Clear()
    {
        var ok = await Shell.Current.DisplayAlert("تأكيد", "هل تريد مسح الإعدادات المحفوظة؟", "نعم", "إلغاء");
        if (!ok) return;

        IsBusy = true;
        try
        {
            await _store.ClearAsync();
            SelectedEnvironment = nameof(NearpayEnvironment.Sandbox);
            SelectedAuthMode = nameof(NearpayAuthMode.UserEnter);
            AuthValue = null;
            Tid = null;
            Locale = "ar-SA";
            ApiKey = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Back()
    {
        await Shell.Current.GoToAsync("..");
    }
}

