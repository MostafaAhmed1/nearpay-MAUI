namespace NearpayPosMauiDemo.App;

public partial class App : Application
{
	private readonly MainPage _mainPage;

	public App(MainPage mainPage)
	{
		InitializeComponent();
		_mainPage = mainPage;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// التطبيق شاشة واحدة للتجربة
		return new Window(_mainPage);
	}
}
