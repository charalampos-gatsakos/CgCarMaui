using Microsoft.Extensions.Logging;

namespace CgCar.App.Services;

/// <summary>
/// Last line of defence: records any exception that nothing else caught, both to the logger and to a crash log
/// file that survives the crash. It does not try to keep the app alive: after an unexpected exception the app's
/// state can't be trusted, so letting it close is safer than continuing.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
{
    private const long MaxLogSizeBytes = 512 * 1024;
    private readonly object _fileLock = new();
    private Exception? _lastLogged;

    public static string CrashLogPath => Path.Combine(FileSystem.AppDataDirectory, "crash.log");

    public void Register()
    {
        // An exception nobody caught, on any thread. The app closes right after this event.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log("Unhandled exception", e.ExceptionObject as Exception);

        // A Task failed and nobody awaited it. This doesn't close the app, but an error went unnoticed.
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log("Unobserved task exception", e.Exception);
            e.SetObserved();
        };

#if ANDROID
        // Exceptions thrown while Android is calling into .NET code (UI events, callbacks) arrive here first.
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
            Log("Unhandled exception (Android)", e.Exception);
#elif IOS
        // Exceptions travelling from .NET back into native iOS code.
        ObjCRuntime.Runtime.MarshalManagedException += (_, e) =>
            Log("Unhandled exception (iOS)", e.Exception);
#endif
    }

    private void Log(string kind, Exception? exception)
    {
        lock (_fileLock)
        {
            // The same exception can be reported by both the platform hook and the AppDomain event.
            if (exception is not null && ReferenceEquals(exception, _lastLogged))
            {
                return;
            }

            _lastLogged = exception;
            logger.LogCritical(exception, "{Kind}", kind);

            try
            {
                var file = new FileInfo(CrashLogPath);
                if (file.Exists && file.Length > MaxLogSizeBytes)
                {
                    file.Delete(); // keep the log small; only recent crashes matter
                }

                File.AppendAllText(
                    CrashLogPath,
                    $"[{DateTime.UtcNow:O}] {kind}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A crash handler must never throw itself; the logger entry above is still there.
            }
        }
    }
}
