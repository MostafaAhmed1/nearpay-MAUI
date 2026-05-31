using NearpayPosMauiDemo.App.Presentation.Main;

namespace NearpayPosMauiDemo.App;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
