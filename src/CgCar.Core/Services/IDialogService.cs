namespace CgCar.Core.Services;

/// <summary>User prompts and notifications, so ViewModels can ask and inform without referencing MAUI pages.</summary>
public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);

    Task AlertAsync(string title, string message);

    /// <summary>Short, non-blocking notification (e.g. "Vehicle saved").</summary>
    Task ToastAsync(string message);
}
