namespace CgCar.Core.Navigation;

/// <summary>
/// Implemented by ViewModels that take navigation parameters. The app's page base class forwards Shell's
/// query attributes here, which keeps ViewModels free of MAUI types (MAUI's own equivalent is IQueryAttributable).
/// </summary>
public interface INavigationParameterReceiver
{
    void ReceiveParameters(IDictionary<string, object> parameters);
}
