namespace CgCar.Core.Data;

/// <param name="DatabasePath">Full path of the SQLite file. The app passes a file in its data folder; tests pass a temp file.</param>
/// <param name="SeedDemoData">Insert sample vehicles the first time the database is created.</param>
public sealed record DatabaseOptions(string DatabasePath, bool SeedDemoData = false);
