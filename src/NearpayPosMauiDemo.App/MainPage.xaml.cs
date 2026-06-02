using NearpayPosMauiDemo.App.Presentation.Main;

namespace NearpayPosMauiDemo.App;

public partial class MainPage : ContentPage
{
	public MainPage() : this(ServiceLocator.Get<MainPageViewModel>())
	{
	}

	public MainPage(MainPageViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
