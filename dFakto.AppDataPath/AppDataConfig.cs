using System.IO;

namespace dFakto.AppDataPath;

/// <summary>
/// Configuration for IAppData / AppData
/// </summary>
public class AppDataConfig
{
    /// <summary>
    /// The path from/to which the AppData instance will read/write files. Paths will be extended to their absolute
    /// representation. If null, a default path will be chosen (typically the classical Application Data path, which
    /// depends on the OS).
    /// </summary>
    public string? BasePath
    {
        get;
        set => field = value is null ? null : Path.GetFullPath(value);
    }
}