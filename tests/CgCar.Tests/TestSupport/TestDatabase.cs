using CgCar.Core.Data;

namespace CgCar.Tests.TestSupport;

/// <summary>A real SQLite repository on a throwaway temp file, deleted when the test finishes.</summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    public TestDatabase(bool seedDemoData = false)
    {
        FilePath = Path.Combine(Path.GetTempPath(), $"cgcar-test-{Guid.NewGuid():N}.db3");
        Repository = new VehicleRepository(new DatabaseOptions(FilePath, seedDemoData));
    }

    public string FilePath { get; }

    public VehicleRepository Repository { get; }

    public async ValueTask DisposeAsync()
    {
        await Repository.DisposeAsync();

        try
        {
            File.Delete(FilePath);
        }
        catch (IOException)
        {
            // A file still locked by the OS is only a leftover temp file.
        }
    }
}
