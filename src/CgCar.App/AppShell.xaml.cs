using CgCar.App.Views;
using CgCar.Core.Navigation;

namespace CgCar.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Shell creates these pages through dependency injection when navigating to their routes.
        Routing.RegisterRoute(Routes.VehicleList, typeof(VehicleListPage));
        Routing.RegisterRoute(Routes.VehicleEdit, typeof(VehicleEditPage));
    }
}
