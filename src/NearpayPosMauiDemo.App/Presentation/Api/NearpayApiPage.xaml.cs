using NearpayPosMauiDemo.App;

namespace NearpayPosMauiDemo.App.Presentation.Api;

public partial class NearpayApiPage : ContentPage
{
    public NearpayApiPage() : this(ServiceLocator.Get<NearpayApiViewModel>())
    {
    }

    public NearpayApiPage(NearpayApiViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}

