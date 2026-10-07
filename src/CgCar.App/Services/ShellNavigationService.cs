using CgCar.Core.Services;

namespace CgCar.App.Services;

/// <summary><see cref="INavigationService"/> implemented with Shell URI navigation.</summary>
public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) =>
        parameters is null
            ? Shell.Current.GoToAsync(route)
            // Single-use parameters: Shell hands them to the target page once and doesn't re-apply them on back navigation.
            : Shell.Current.GoToAsync(route, new ShellNavigationQueryParameters(parameters));

    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
