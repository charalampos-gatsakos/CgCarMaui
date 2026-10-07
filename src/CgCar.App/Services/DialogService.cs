using CgCar.Core.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace CgCar.App.Services;

/// <summary><see cref="IDialogService"/> using native alert dialogs and CommunityToolkit.Maui toasts.</summary>
public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        Shell.Current.DisplayAlertAsync(title, message, accept, cancel);

    public Task AlertAsync(string title, string message) =>
        Shell.Current.DisplayAlertAsync(title, message, "OK");

    public Task ToastAsync(string message) =>
        Toast.Make(message, ToastDuration.Short).Show();
}
