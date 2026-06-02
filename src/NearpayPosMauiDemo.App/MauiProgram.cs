using Microsoft.Extensions.Logging;
using NearpayPosMauiDemo.App.Presentation.Main;
using NearpayPosMauiDemo.App.Presentation.Api;
using NearpayPosMauiDemo.App.Presentation.Settings;
using NearpayPosMauiDemo.App.Services;
using NearpayPosMauiDemo.Core.Abstractions;

namespace NearpayPosMauiDemo.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if ANDROID
		builder.Services.AddSingleton<INearpayService, Platforms.Android.Services.NearpayServiceAndroid>();
#else
		builder.Services.AddSingleton<INearpayService, NearpayServiceStub>();
#endif

		builder.Services.AddSingleton<INearpaySettingsStore, NearpaySettingsStore>();
		builder.Services.AddSingleton(new HttpClient());
		builder.Services.AddSingleton<INearpayApiClient, NearpayApiClient>();

		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();

		builder.Services.AddSingleton<NearpayApiViewModel>();
		builder.Services.AddSingleton<NearpayApiPage>();
		builder.Services.AddSingleton<NearpaySettingsViewModel>();
		builder.Services.AddSingleton<NearpaySettingsPage>();
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
