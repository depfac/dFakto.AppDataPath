using System;
using System.IO;

namespace dFakto.AppDataPath;

public interface IAppData
{
    /// <summary>
    /// The path to the root directory of the application base path.
    /// </summary>
    string BasePath { get; }
    /// <summary>
    /// The path to the "configuration" directory of the application base path, which is a subdirectory of
    /// the base path. Typically, this directory contains JSON files that are loaded at application startup.
    /// </summary>
    string ConfigPath { get; }
    /// <summary>
    /// The path to the "temporary" directory of the application base path, which is a subdirectory of the base path.
    /// This directory should contain temporary files. Typically, this directory is purged at application startup.
    /// </summary>
    string TempPath { get; }
    /// <summary>
    /// The path to the "data" directory of the application base path, which is a subdirectory of the base path.
    /// This directory should contain persistent files. When migrations are performed, this directory is generally
    /// affected.
    /// </summary>
    string DataPath { get; }

    /// <summary>
    /// Get the version of the appdata path. If the file cannot be read for any reason, the version will be 0.0
    /// </summary>
    Version CurrentVersion { get; }

    /// <summary>
    /// Generate a file/directory path inside the app data path. The list of path segments are joined and prefixed
    /// by "Basepath" to form the resulting path. This is *only* a path, no files or directories are actually created.
    /// It is forbidden to generate a path outside the application base path.
    /// </summary>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the resulting path is outside the application base path.</exception>
    string GetFilePath(params string[] pathSegments);
    /// <summary>
    /// Generate a file/directory path inside the app data path. The list of path segments are joined and prefixed
    /// by "Basepath" and by the name of the standard application base path identified by the "dir" parameter to form
    /// the resulting path. This is *only* a path, no files or directories are actually created.
    /// It is forbidden to generate a path outside the application base path.
    /// </summary>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the resulting path is outside the application base path.</exception>
    string GetFilePath(AppDataDir dir, params string[] pathSegments);

    /// <summary>
    /// Generate a file/directory path inside the app data path. The list of path segments are joined and prefixed
    /// by "Basepath" to form the resulting path. The parent directory(-ies) of the resulting path are created using
    /// <see cref="DirectoryInfo.Create()"/>.
    /// It is forbidden to generate a path outside the application base path.
    /// </summary>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the resulting path is outside the application base path.</exception>
    string GetFilePathAndCreateParents(params string[] pathSegments);
    /// <summary>
    /// Generate a file/directory path inside the app data path. The list of path segments are joined and prefixed
    /// by "Basepath" and by the name of the standard application base path identified by the "dir" parameter to form
    /// the resulting path. The parent directory(-ies) of the resulting path are created using
    /// <see cref="DirectoryInfo.Create()"/>.
    /// It is forbidden to generate a path outside the application base path.
    /// </summary>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the resulting path is outside the application base path.</exception>
    string GetFilePathAndCreateParents(AppDataDir dir, params string[] pathSegments);

    /// <summary>
    /// Read the contents of the file using <see cref="File.ReadAllText(string)"/>. If the file does not exist,
    /// this returns an empty string.
    /// </summary>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the file path is outside the application base path.</exception>
    string ReadAllText(params string[] pathSegments);
    /// <summary>
    /// Read the contents of the file using <see cref="File.ReadAllText(string)"/>. If the file does not exist,
    /// this returns an empty string.
    /// </summary>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the file path is outside the application base path.</exception>
    string ReadAllText(AppDataDir dir, params string[] pathSegments);

    /// <summary>
    /// Write the contents to the file using <see cref="File.WriteAllText(string, string)"/>. If the file does not
    /// exist, it is created, as are all its parent directories.
    /// </summary>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the file path is outside the application base path.</exception>
    void WriteAllText(string contents, params string[] pathSegments);
    /// <summary>
    /// Write the contents to the file using <see cref="File.WriteAllText(string, string)"/>. If the file does not
    /// exist, it is created, as are all its parent directories.
    /// </summary>
    /// <param name="contents">The contents to write to the file.</param>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If file path is outside the application base path.</exception>
    void WriteAllText(string contents, AppDataDir dir, params string[] pathSegments);

    /// <summary>
    /// Append the contents to the file using <see cref="File.AppendAllText(string, string)"/>. If the file does not
    /// exist, it is created, as are all its parent directories.
    /// </summary>
    /// <param name="contents">The contents to append to the file.</param>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the file path is outside the application base path.</exception>
    void AppendAllText(string contents, params string[] pathSegments);
    /// <summary>
    /// Append the contents to the file using <see cref="File.AppendAllText(string, string)"/>. If the file does not
    /// exist, it is created, as are all its parent directories.
    /// </summary>
    /// <param name="contents">The contents to append to the file.</param>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If file path is outside the application base path.</exception>
    void AppendAllText(string contents, AppDataDir dir, params string[] pathSegments);

    /// <summary>
    /// Delete the file using <see cref="File.Delete(string)"/>. If the file does not exist, this does nothing.
    /// </summary>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the file path is outside the application base path.</exception>
    void DeleteFile(params string[] pathSegments);
    /// <summary>
    /// Delete the file using <see cref="File.Delete(string)"/>. If the file does not exist, this does nothing.
    /// </summary>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the file path.</param>
    /// <exception cref="ArgumentException">If the file path is outside the application base path.</exception>
    void DeleteFile(AppDataDir dir, params string[] pathSegments);

    /// <summary>
    /// Delete a directory recursively.
    /// </summary>
    /// <param name="excludeTopDir">Do not delete the directory itself, delete the contents only.</param>
    /// <param name="force">Before trying to delete files, explicitly set them to <see cref="FileAttributes.Normal"/>.
    /// This avoids blocking the operation when read-only files are present. </param>
    /// <param name="pathSegments">The list of segments in the directory path.</param>
    /// <exception cref="ArgumentException">If the file path is outside the application base path.</exception>
    public void DeleteDirectory(bool excludeTopDir, bool force, params string[] pathSegments);
    /// <summary>
    /// Delete a directory recursively.
    /// </summary>
    /// <param name="excludeTopDir">Do not delete the directory itself, delete the contents only.</param>
    /// <param name="force">Before trying to delete files, explicitly set them to <see cref="FileAttributes.Normal"/>.
    /// This avoids blocking the operation when read-only files are present. </param>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the directory path.</param>
    /// <exception cref="ArgumentException">If the file path is outside the application base path.</exception>
    public void DeleteDirectory(bool excludeTopDir, bool force, AppDataDir dir, params string[] pathSegments);

    /// <summary>
    /// Get the list of files in a directory using <see cref="Directory.GetFiles(string)"/>.
    /// </summary>
    /// <param name="pathSegments">The list of segments in the directory path.</param>
    /// <returns>The list of paths file paths.</returns>
    public string[] GetDirectoryFiles(params string[] pathSegments);
    /// <summary>
    /// Get the list of files in a directory using <see cref="Directory.GetFiles(string)"/>.
    /// </summary>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the directory path.</param>
    /// <returns>The list of file paths.</returns>
    public string[] GetDirectoryFiles(AppDataDir dir, params string[] pathSegments);

    /// <summary>
    /// Get the list of files in a directory using <see cref="Directory.GetFiles(string, string, SearchOption)"/>.
    /// </summary>
    /// <param name="searchPattern">Parameter of <see cref="Directory.GetFiles(string, string, SearchOption)"/></param>
    /// <param name="options">Parameter of <see cref="Directory.GetFiles(string, string, SearchOption)"/></param>
    /// <param name="pathSegments">The list of segments in the directory path.</param>
    /// <returns>The list of paths of matching files.</returns>
    public string[] GetDirectoryFiles(string searchPattern, SearchOption options, params string[] pathSegments);
    /// <summary>
    /// Get the list of files in a directory using <see cref="Directory.GetFiles(string, string, SearchOption)"/>.
    /// </summary>
    /// <param name="searchPattern">Parameter of <see cref="Directory.GetFiles(string, string, SearchOption)"/></param>
    /// <param name="options">Parameter of <see cref="Directory.GetFiles(string, string, SearchOption)"/></param>
    /// <param name="dir">The standard directory name used as a prefix to the other path segments.</param>
    /// <param name="pathSegments">The list of segments in the directory path.</param>
    /// <returns>The list of paths of matching files.</returns>
    public string[] GetDirectoryFiles(
        string searchPattern, SearchOption options, AppDataDir dir, params string[] pathSegments);
}