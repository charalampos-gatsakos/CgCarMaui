using CgCar.Core.Services;

namespace CgCar.Tests.TestSupport;

/// <summary>Records navigation calls instead of navigating.</summary>
internal sealed class FakeNavigationService : INavigationService
{
    public List<(string Route, IDictionary<string, object>? Parameters)> Navigations { get; } = [];

    public int BackNavigations { get; private set; }

    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        Navigations.Add((route, parameters));
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        BackNavigations++;
        return Task.CompletedTask;
    }
}

/// <summary>Answers confirmations with <see cref="ConfirmResult"/> and records alerts and toasts.</summary>
internal sealed class FakeDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;

    public int Confirmations { get; private set; }

    public List<string> Alerts { get; } = [];

    public List<string> Toasts { get; } = [];

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        Confirmations++;
        return Task.FromResult(ConfirmResult);
    }

    public Task AlertAsync(string title, string message)
    {
        Alerts.Add(message);
        return Task.CompletedTask;
    }

    public Task ToastAsync(string message)
    {
        Toasts.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>Simulates the gallery and photo storage without touching the file system.</summary>
internal sealed class FakePhotoService : IPhotoService
{
    /// <summary>What the next "gallery pick" returns; null simulates the user cancelling.</summary>
    public string? NextPick { get; set; }

    public List<string> Saved { get; } = [];

    public List<string> Deleted { get; } = [];

    public Task<string?> PickPhotoAsync() => Task.FromResult(NextPick);

    public Task<string> SavePhotoAsync(string temporaryPath)
    {
        var reference = $"photos/{Path.GetFileName(temporaryPath)}";
        Saved.Add(reference);
        return Task.FromResult(reference);
    }

    public string? GetFullPath(string? reference) => reference is null ? null : $"/data/{reference}";

    public void DeletePhoto(string? reference)
    {
        if (reference is not null)
        {
            Deleted.Add(reference);
        }
    }
}
