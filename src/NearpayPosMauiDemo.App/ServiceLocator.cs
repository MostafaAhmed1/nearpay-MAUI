using Microsoft.Extensions.DependencyInjection;

namespace NearpayPosMauiDemo.App;

public static class ServiceLocator
{
    public static IServiceProvider Services
        => Application.Current?.Handler?.MauiContext?.Services
           ?? throw new InvalidOperationException("ServiceProvider غير متاح بعد.");

    public static T Get<T>() where T : notnull
        => Services.GetRequiredService<T>();
}

