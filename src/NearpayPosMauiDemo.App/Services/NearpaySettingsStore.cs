using Microsoft.Maui.Storage;

namespace NearpayPosMauiDemo.App.Services;

public sealed record NearpayLocalSettings(
    string Environment,
    string AuthMode,
    string? AuthValue,
    string? Tid,
    string? Locale,
    string? ApiKey
);

public interface INearpaySettingsStore
{
    Task<NearpayLocalSettings> LoadAsync();
    Task SaveAsync(NearpayLocalSettings settings);
    Task ClearAsync();
}

public sealed class NearpaySettingsStore : INearpaySettingsStore
{
    // Preferences (غير حساسة)
    private const string PrefEnv = "Nearpay.Env";
    private const string PrefAuthMode = "Nearpay.AuthMode";
    private const string PrefTid = "Nearpay.Tid";
    private const string PrefLocale = "Nearpay.Locale";

    // SecureStorage (حساسة)
    private const string SecAuthValue = "Nearpay.AuthValue";
    private const string SecApiKey = "Nearpay.ApiKey";

    public async Task<NearpayLocalSettings> LoadAsync()
    {
        var env = Preferences.Default.Get(PrefEnv, "Sandbox");
        var mode = Preferences.Default.Get(PrefAuthMode, "UserEnter");
        var tid = Preferences.Default.Get(PrefTid, string.Empty);
        var locale = Preferences.Default.Get(PrefLocale, "ar-SA");

        string? authValue = null;
        string? apiKey = null;
        try
        {
            authValue = await SecureStorage.Default.GetAsync(SecAuthValue);
            apiKey = await SecureStorage.Default.GetAsync(SecApiKey);
        }
        catch
        {
            // بعض الأجهزة/ROM قد تمنع SecureStorage؛ نترك القيم فارغة ونسمح للمستخدم بالإدخال.
        }

        return new NearpayLocalSettings(env, mode, authValue, tid, locale, apiKey);
    }

    public async Task SaveAsync(NearpayLocalSettings settings)
    {
        Preferences.Default.Set(PrefEnv, settings.Environment);
        Preferences.Default.Set(PrefAuthMode, settings.AuthMode);
        Preferences.Default.Set(PrefTid, settings.Tid ?? string.Empty);
        Preferences.Default.Set(PrefLocale, settings.Locale ?? "ar-SA");

        // لا نريد حفظ secrets في Preferences
        try
        {
            if (settings.AuthValue is null)
                SecureStorage.Default.Remove(SecAuthValue);
            else
                await SecureStorage.Default.SetAsync(SecAuthValue, settings.AuthValue);

            if (settings.ApiKey is null)
                SecureStorage.Default.Remove(SecApiKey);
            else
                await SecureStorage.Default.SetAsync(SecApiKey, settings.ApiKey);
        }
        catch
        {
            // ignore
        }
    }

    public Task ClearAsync()
    {
        Preferences.Default.Remove(PrefEnv);
        Preferences.Default.Remove(PrefAuthMode);
        Preferences.Default.Remove(PrefTid);
        Preferences.Default.Remove(PrefLocale);

        try
        {
            SecureStorage.Default.Remove(SecAuthValue);
            SecureStorage.Default.Remove(SecApiKey);
        }
        catch
        {
            // ignore
        }

        return Task.CompletedTask;
    }
}

