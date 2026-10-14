using System;

namespace dFakto.AppDataPath;

/// <summary>
/// Represents the collection of actions to perform on an application path to migrate to <see cref="Version"/> from the
/// version preceding it. An app should override this for every migration of application data.
/// </summary>
public interface IAppDataMigration
{
    /// <summary>
    /// Should be overriden to return the Version this targets.
    /// </summary>
    Version Version { get; }
    /// <summary>
    /// This is run to migrate to version <see cref="Version"/>
    /// </summary>
    void Upgrade(IAppData appData, IServiceProvider serviceProvider);
}