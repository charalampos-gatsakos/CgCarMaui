using CgCar.App.Services;
using CgCar.App.Views;
using CgCar.Core.Data;
using CgCar.Core.Services;
using CgCar.Core.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace CgCar.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Data: one repository (and SQLite connection) for the whole app.
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "cgcar.db3");
        builder.Services.AddSingleton(new DatabaseOptions(databasePath, SeedDemoData: true));
        builder.Services.AddSingleton<IVehicleRepository, VehicleRepository>();

        // Platform services behind the Core interfaces.
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IPhotoService, PhotoService>();
        builder.Services.AddSingleton<GlobalExceptionHandler>();

        // Pages and ViewModels. The dashboard is the root page and lives as long as the app (singleton);
        // list and edit pages are created fresh on every navigation (transient), so each starts with clean state.
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddTransient<VehicleListViewModel>();
        builder.Services.AddTransient<VehicleListPage>();
        builder.Services.AddTransient<VehicleEditViewModel>();
        builder.Services.AddTransient<VehicleEditPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Hook up last-resort exception logging as early as possible, before any page runs.
        app.Services.GetRequiredService<GlobalExceptionHandler>().Register();

        return app;
    }
}
