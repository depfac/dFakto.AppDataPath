using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("dFakto.AppDataPath.Tests")]

namespace dFakto.AppDataPath;

/// <summary>
/// See: <see cref="IAppData"/>
/// </summary>
public class AppData : IAppData
{
    internal const string VersionFileName = "VERSION.txt";

    private readonly AppDataConfig _config;
    private string? _defaultBasePathCached;

    /// <summary>
    /// Build a new appdata path instance.
    /// </summary>
    /// <param name="config">The appdata configuration parameters.</param>
    public AppData(AppDataConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// See: <see cref="IAppData.BasePath"/>
    /// </summary>
    public string BasePath => _config.BasePath ?? GetDefaultBasePath();
    /// <summary>
    /// See: <see cref="IAppData.ConfigPath"/>
    /// </summary>
    public string ConfigPath => GetFilePath(AppDataDir.Config);
    /// <summary>
    /// See: <see cref="IAppData.TempPath"/>
    /// </summary>
    public string TempPath => GetFilePath(AppDataDir.Temp);
    /// <summary>
    /// See: <see cref="IAppData.DataPath"/>
    /// </summary>
    public string DataPath => GetFilePath(AppDataDir.Data);

    /// <summary>
    /// See: <see cref="IAppData.CurrentVersion"/>
    /// </summary>
    public Version CurrentVersion => Version.TryParse(ReadAllText(VersionFileName), out var result)
        ? result
        : new Version();

    /// <summary>
    /// See: <see cref="IAppData.GetFilePath(string[])"/>
    /// </summary>
    public string GetFilePath(params string[] pathSegments)
    {
        var segments = Prepend(BasePath, pathSegments);
        var combinedPath = Path.GetFullPath(Path.Combine(segments));

        if (!IsPathTraversalSafe(combinedPath))
        {
            throw new ArgumentException($"Specified file path escapes AppDataPath: {combinedPath}");
        }

        return combinedPath;
    }

    /// <summary>
    /// See: <see cref="IAppData.GetFilePath(AppDataDir, string[])"/>
    /// </summary>
    public string GetFilePath(AppDataDir dir, params string[] pathSegments)
    {
        return GetFilePath(Prepend(dir.ToDirectoryName(), pathSegments));
    }

    /// <summary>
    /// See: <see cref="IAppData.GetFilePathAndCreateParents(string[])"/>
    /// </summary>
    public string GetFilePathAndCreateParents(params string[] pathSegments)
    {
        var path = GetFilePath(pathSegments);
        CheckAndCreateParentDirectory(path);
        return path;
    }

    /// <summary>
    /// See: <see cref="IAppData.GetFilePathAndCreateParents(AppDataDir, string[])"/>
    /// </summary>
    public string GetFilePathAndCreateParents(AppDataDir dir, params string[] pathSegments)
    {
        var path = GetFilePath(dir, pathSegments);
        CheckAndCreateParentDirectory(path);
        return path;
    }

    /// <summary>
    /// See: <see cref="IAppData.ReadAllText(string[])"/>
    /// </summary>
    public string ReadAllText(params string[] pathSegments)
    {
        var path = GetFilePath(pathSegments);

        if  (!File.Exists(path))
            return string.Empty;

        return File.ReadAllText(path);
    }

    /// <summary>
    /// See: <see cref="IAppData.ReadAllText(AppDataDir, string[])"/>
    /// </summary>
    public string ReadAllText(AppDataDir dir, params string[] pathSegments)
    {
        return ReadAllText(Prepend(dir.ToDirectoryName(), pathSegments));
    }

    /// <summary>
    /// See: <see cref="IAppData.WriteAllText(string, string[])"/>
    /// </summary>
    public void WriteAllText(string contents, params string[] pathSegments)
    {
        var path = GetFilePath(pathSegments);
        CheckAndCreateParentDirectory(path);
        File.WriteAllText(path, contents);
    }

    /// <summary>
    /// See: <see cref="IAppData.WriteAllText(string, AppDataDir, string[])"/>
    /// </summary>
    public void WriteAllText(string contents, AppDataDir dir, params string[] pathSegments)
    {
        var path = GetFilePath(dir, pathSegments);
        CheckAndCreateParentDirectory(path);
        File.WriteAllText(path, contents);
    }

    /// <summary>
    /// See: <see cref="IAppData.AppendAllText(string, string[])"/>
    /// </summary>
    public void AppendAllText(string contents, params string[] pathSegments)
    {
        var path = GetFilePath(pathSegments);
        CheckAndCreateParentDirectory(path);
        File.AppendAllText(path, contents);
    }

    /// <summary>
    /// See: <see cref="IAppData.AppendAllText(string, AppDataDir, string[])"/>
    /// </summary>
    public void AppendAllText(string contents, AppDataDir dir, params string[] pathSegments)
    {
        var path = GetFilePath(dir, pathSegments);
        CheckAndCreateParentDirectory(path);
        File.AppendAllText(path, contents);
    }

    /// <summary>
    /// See: <see cref="IAppData.DeleteFile(string[])"/>
    /// </summary>
    public void DeleteFile(params string[] pathSegments)
    {
        var path = GetFilePath(pathSegments);
        if (File.Exists(path))
            File.Delete(path);
    }

    /// <summary>
    /// See: <see cref="IAppData.DeleteFile(AppDataDir, string[])"/>
    /// </summary>
    public void DeleteFile(AppDataDir dir, params string[] pathSegments)
    {
        var path = GetFilePath(dir, pathSegments);
        if (File.Exists(path))
            File.Delete(path);
    }

    /// <summary>
    /// See: <see cref="IAppData.DeleteDirectory(bool, bool, string[])"/>
    /// </summary>
    public void DeleteDirectory(bool excludeTopDir, bool force, params string[] pathSegments)
    {
        var path = GetFilePath(pathSegments);

        if (Directory.Exists(path))
        {
            var directoryInfo = new DirectoryInfo(path);

            if (force)
                SetDirectoryContentsAttributesNormal(directoryInfo);

            DeleteDirectoryContents(directoryInfo);

            if (!excludeTopDir)
                directoryInfo.Delete(true);
        }
    }

    /// <summary>
    /// See: <see cref="IAppData.DeleteDirectory(bool, bool, AppDataDir, string[])"/>
    /// </summary>
    public void DeleteDirectory(bool excludeTopDir, bool force, AppDataDir dir, params string[] pathSegments)
    {
        DeleteDirectory(excludeTopDir, force, Prepend(dir.ToDirectoryName(), pathSegments));
    }

    /// <summary>
    /// See: <see cref="IAppData.GetDirectoryFiles(string[])"/>
    /// </summary>
    public string[] GetDirectoryFiles(params string[] pathSegments)
    {
        var path = GetFilePath(pathSegments);
        if (!Directory.Exists(path))
            return Array.Empty<string>();
        return Directory.GetFiles(path);
    }

    /// <summary>
    /// See: <see cref="IAppData.GetDirectoryFiles(AppDataDir, string[])"/>
    /// </summary>
    public string[] GetDirectoryFiles(AppDataDir dir, params string[] pathSegments)
    {
        return GetDirectoryFiles(Prepend(dir.ToDirectoryName(), pathSegments));
    }

    /// <summary>
    /// See: <see cref="IAppData.GetDirectoryFiles(string, SearchOption, string[])"/>
    /// </summary>
    public string[] GetDirectoryFiles(string searchPattern, SearchOption options, params string[] pathSegments)
    {
        var path = GetFilePath(pathSegments);
        if (!Directory.Exists(path))
            return Array.Empty<string>();
        return Directory.GetFiles(path, searchPattern, options);
    }

    /// <summary>
    /// See: <see cref="IAppData.GetDirectoryFiles(string, SearchOption, AppDataDir, string[])"/>
    /// </summary>
    public string[] GetDirectoryFiles(
        string searchPattern, SearchOption options, AppDataDir dir, params string[] pathSegments)
    {
        return GetDirectoryFiles(searchPattern, options, Prepend(dir.ToDirectoryName(), pathSegments));
    }

    /// <summary>
    /// Helper function that returns true if the given path is contained in the appdata path folder. Paths generated
    /// by the library should be checked against this function to make sure path traversal is protected against.
    /// </summary>
    private bool IsPathTraversalSafe(string path)
    {
        var basePath = BasePath;

        if (basePath == string.Empty || basePath[basePath.Length - 1] != Path.DirectorySeparatorChar)
            basePath += Path.DirectorySeparatorChar;

        return new Uri(basePath).IsBaseOf(new Uri(path + Path.DirectorySeparatorChar));
    }

    /// <summary>
    /// Helper function. Returns a new array that consists of: [s, stringArray[0], stringArray[1], ...]
    /// </summary>
    private static string[] Prepend(string s, string[] stringArray)
    {
        var newArray = new string[stringArray.Length + 1];
        newArray[0] = s;
        stringArray.CopyTo(newArray, 1);
        return newArray;
    }

    /// <summary>
    /// Create parent directories of the given file or directory path, if they do not exist.
    /// </summary>
    private static void CheckAndCreateParentDirectory(string path)
    {
        var parentPath = Directory.GetParent(path);

        if (!(parentPath is null) && !parentPath.Exists)
            parentPath.Create();
    }

    /// <summary>
    /// Deletes the contents of a directory. This does not delete the directory itself.
    /// </summary>
    private static void DeleteDirectoryContents(DirectoryInfo directoryInfo)
    {
        foreach (var file in directoryInfo.GetFiles())
            file.Delete();

        foreach (var directory in directoryInfo.GetDirectories())
            directory.Delete(true);
    }

    /// <summary>
    /// Set the attributes of the contents of a directory to "Normal", i.e. not "ReadOnly".
    /// If readonly files exists in a dir, recursive suppression may fail.
    /// </summary>
    private static void SetDirectoryContentsAttributesNormal(DirectoryInfo directory)
    {
        foreach (var subDir in directory.GetDirectories())
            SetDirectoryContentsAttributesNormal(subDir);

        foreach (var file in directory.GetFiles())
            File.SetAttributes(file.FullName, FileAttributes.Normal);
    }

    private string GetDefaultBasePath()
    {
        // Return cache if we have it
        if (!(_defaultBasePathCached is null))
            return _defaultBasePathCached;

        // We don't actually care that the path to the application data exists or not, we want to
        // fail as late as possible. (On an actual data file access for example.)
        var systemApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        // We try to get the name of the assembly.
        var executableName = Assembly.GetEntryAssembly()?.GetName().Name;

        if (string.IsNullOrWhiteSpace(executableName))
        {
            try
            {
                // Otherwise we try to get the name of the process
                executableName = Process.GetCurrentProcess().ProcessName;
            }
            catch
            {
                // Keep variable at null if we cannot retrieve process name
            }
        }

        if (string.IsNullOrWhiteSpace(executableName))
        {
            // We can't get a reproducible directory name, fail with an exception
            throw new InvalidOperationException(
                "Failed to identify a reproducible default application directory path.");
        }

        // Cache result
        _defaultBasePathCached = Path.Combine(systemApplicationDataPath, executableName);

        return _defaultBasePathCached;
    }
}