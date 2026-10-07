using CgCar.Core.ViewModels;

namespace CgCar.App.Views;

public partial class VehicleListPage : BasePage
{
    public VehicleListPage(VehicleListViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
