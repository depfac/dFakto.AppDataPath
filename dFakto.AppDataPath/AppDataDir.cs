using System;

namespace dFakto.AppDataPath;

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
