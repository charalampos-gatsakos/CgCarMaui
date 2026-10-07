using CgCar.Core.ViewModels;

namespace CgCar.App.Views;

public partial class VehicleEditPage : BasePage
{
    public VehicleEditPage(VehicleEditViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
