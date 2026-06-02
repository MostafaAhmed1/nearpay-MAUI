using NearpayPosMauiDemo.App;

namespace NearpayPosMauiDemo.App.Presentation.Settings;

public partial class NearpaySettingsPage : ContentPage
{
    public NearpaySettingsPage() : this(ServiceLocator.Get<NearpaySettingsViewModel>())
    {
    }

    public NearpaySettingsPage(NearpaySettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
