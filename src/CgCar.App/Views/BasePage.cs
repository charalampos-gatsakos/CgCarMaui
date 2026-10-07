using CgCar.Core.Navigation;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CgCar.App.Views;

/// <summary>
/// Base class for the app's pages: sets the ViewModel as BindingContext and forwards Shell's navigation
/// parameters to it. The ViewModels live in CgCar.Core and can't implement MAUI's IQueryAttributable themselves.
/// </summary>
public abstract class BasePage : ContentPage, IQueryAttributable
{
    protected BasePage(object viewModel)
    {
        BindingContext = viewModel;

        // Status bar matches the navigation bar in both light and dark theme.
        var statusBar = new StatusBarBehavior { StatusBarStyle = StatusBarStyle.LightContent };
        statusBar.SetAppThemeColor(StatusBarBehavior.StatusBarColorProperty, AppColor("Primary"), AppColor("SurfaceDark"));
        Behaviors.Add(statusBar);
    }

    void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is INavigationParameterReceiver receiver)
        {
            receiver.ReceiveParameters(query);
        }
    }

    private static Color AppColor(string key) => (Color)Application.Current!.Resources[key];
}
