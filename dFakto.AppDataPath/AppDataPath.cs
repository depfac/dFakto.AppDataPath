using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

[assembly:InternalsVisibleTo("dFakto.AppDataPath.Tests")]

namespace dFakto.AppDataPath
{
    public class AppData : IDisposable
    {
        private const string VersionFileName = "VERSION.txt";
        
        private const string ConfigPathName = "config";
        private const string TempPathName = "temp";
        private const string DataPathName = "data";

        private readonly ILogger<AppData>? _logger;
        private readonly AppDataConfig _config;

        public string BasePath;

        internal string ConfigPath => Path.Combine(BasePath, ConfigPathName);
        internal string TempPath => Path.Combine(BasePath, TempPathName);
        internal string DataPath => Path.Combine(BasePath, DataPathName);

        public Version CurrentVersion => GetCurrentVersion();

        public AppData(ILogger<AppData>? logger, AppDataConfig config)
        {
            _config = config ?? throw new ArgumentException(nameof(config));
            BasePath = config.BasePath ?? GetDefaultBasePath();
            _logger = logger;

            Directory.CreateDirectory(TempPath);
            Directory.CreateDirectory(ConfigPath);
            Directory.CreateDirectory(DataPath);

            // Logger may be null when loading configuration
            _logger?.LogInformation($"Using '{BasePath}' as Application BasePath (Version : {CurrentVersion})");

            // Cleanup temp directory from eventual remaining files
            _logger?.LogInformation($"Cleaning '{TempPath}' for application startup");
            EmptyTemp();
        }

        private static string GetDefaultBasePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.None),
                System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "");
        }

        /// <summary>
        /// Returns a FileName within the data folder.
        /// The Directory is automtically created
        /// </summary>
        /// <param name="tokens">Path tokens</param>
        /// <returns>Data File Path</returns>
        public string GetDataFileName(params string[] tokens)
        {
            var elems = new List<string> {DataPath};
            elems.AddRange(tokens);
            string path = Path.Combine(elems.ToArray());
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());
            return path;
        }

        /// <summary>
        /// Open a Temporary filestream. The file will be deleted on close.
        /// </summary>
        /// <returns>an open R/W FileStream</returns>
        public FileStream OpenTempFile()
        {
            return new FileStream(GetTempFileName(),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                4096, FileOptions.RandomAccess | FileOptions.DeleteOnClose);
        }

        /// <summary>
        /// Return a Temporary filename. The path will point into the temp folder of BasePath.
        /// </summary>
        /// <returns>Temporary file path</returns>
        public string GetTempFileName()
        {
            return Path.Combine(TempPath, Path.GetRandomFileName());
        }

        /// <summary>
        /// Delete the complete Temporary folder content, useful at launch to cleanup any remaining temporary files of
        /// an eventual crash
        /// </summary>
        public void EmptyTemp()
        {
            new DirectoryInfo(TempPath).DeleteAllContent();
        }

        /// <summary>
        /// Delete the complete Application folder content, including data and configuration. Use with care !
        /// </summary>
        public void EmptyAll()
        {
            new DirectoryInfo(BasePath).DeleteAllContent();
        }
        
        internal IEnumerable<string> GetConfigFileNames()
        {
            return Directory.GetFiles(ConfigPath);
        }

        public void Dispose()
        {
            if (_config.CleanupTempFileOnClose)
            {
                try
                {
                    _logger?.LogInformation("Emptying '{TempPath}'",TempPath);
                    EmptyTemp();
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Unable to empty '{TempPath}'",TempPath);
                }
            }
        }
        
        private Version GetCurrentVersion()
        {
            string versionFilePath = GetCurrentVersionFileName();
            
            if (!File.Exists(versionFilePath))
                return new Version();
            return Version.Parse(File.ReadAllText(versionFilePath));
        }
        
        internal void SetCurrentVersion(Version version)
        {
            _logger?.LogInformation("AppData version set to: {Version}",version);
            File.WriteAllText( GetCurrentVersionFileName(), version.ToString());
        }

        private string GetCurrentVersionFileName()
        {
            return Path.Combine(BasePath, VersionFileName);
        }
    }
}
