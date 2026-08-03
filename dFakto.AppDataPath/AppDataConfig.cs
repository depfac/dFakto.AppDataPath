using System.IO;

namespace dFakto.AppDataPath;

public class AppDataConfig
{
    public string? BasePath
    {
        get;
        set => field = value is null ? null : Path.GetFullPath(value);
    }
}