using System;

namespace dFakto.AppDataPath;

public interface IAppDataMigration
{
    /// <summary>
    /// Should be overriden by the Version this migration migrates to.
    /// </summary>
    Version Version { get; }
    /// <summary>
    /// Run the migration to be compatible with version <see cref="Version"/>
    /// </summary>
    void Upgrade(IAppData appData, IServiceProvider serviceProvider);
}