using CgCar.Core.ViewModels;

namespace CgCar.App.Views;

public partial class DashboardPage : BasePage
{
    public DashboardPage(DashboardViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
