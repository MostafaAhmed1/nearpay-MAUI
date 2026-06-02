namespace NearpayPosMauiDemo.App;

public partial class App : Application
{
	private readonly AppShell _shell;

	public App(AppShell shell)
	{
		InitializeComponent();
		_shell = shell;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// استخدام Shell لتمكين صفحة إعدادات منفصلة بدون كسر وظائف الصفحة الرئيسية
		return new Window(_shell);
	}
}
