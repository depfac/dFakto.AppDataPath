using System.Collections.Generic;

namespace dFakto.AppDataPath;

public interface IAppDataMigrationProvider
{
    /// <summary>
    /// Get the list of existing migrations. Appdata path then handles: the orchestration, filtering of old versions,
    /// sorting, and checking for duplicates.
    /// </summary>
    IEnumerable<IAppDataMigration> GetAppDataMigrations();
}