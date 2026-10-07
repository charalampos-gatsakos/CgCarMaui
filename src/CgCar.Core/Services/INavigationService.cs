namespace CgCar.Core.Services;

/// <summary>
/// Page navigation as seen by ViewModels. The MAUI app implements it with Shell;
/// tests use a fake that just records the calls.
/// </summary>
public interface INavigationService
{
    /// <summary>Navigates to a registered route (see <see cref="Navigation.Routes"/>), passing optional parameters.</summary>
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);

    Task GoBackAsync();
}
