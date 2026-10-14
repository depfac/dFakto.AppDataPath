using System;

namespace dFakto.AppDataPath;

/// <summary>
/// Represents a few default locations for application data. Typically, these are subdirectories to the base path.
/// One could imagine future implementations where they are completely separate. They are resolved at runtime.
/// </summary>
public enum AppDataDir
{
    /// <summary>
    /// Identifies the configuration directory. JSON files in this directory are typically loaded as part of the host's
    /// configuration.
    /// </summary>
    Config,
    /// <summary>
    /// Identifies the data directory. Files inside this directory are persisted.
    /// </summary>
    Data,
    /// <summary>
    /// Identifies the temporary file directory. Files inside this directory are purged at startup.
    /// </summary>
    Temp
}

/// <summary>
/// Extensions for AppDataDir.
/// </summary>
public static class DefaultFolderExtensions
{
    /// <summary>
    /// Get the name of the standard directories of the application base path.
    /// </summary>
    public static string ToDirectoryName(this AppDataDir folder) => folder switch
    {
        AppDataDir.Temp => "temp",
        AppDataDir.Data => "data",
        AppDataDir.Config => "config",
        _ => throw new ArgumentOutOfRangeException(nameof(folder), folder, null)
    };
}
