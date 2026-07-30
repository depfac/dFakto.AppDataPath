using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("dFakto.AppDataPath.Tests")]

namespace dFakto.AppDataPath
{
    /// <summary>
    /// Main Class used to manage Application data files
    /// </summary>
    public sealed class AppData : IDisposable
    {
        private const string VersionFileName = "VERSION.txt";

        private const string ConfigPathName = "config";
        private const string TempPathName = "temp";
        private const string DataPathName = "data";
        private readonly AppDataConfig _config;

        private readonly ILogger<AppData> _logger;

        public AppData(ILogger<AppData> logger, AppDataConfig config)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(config);

            _config = config;
            _logger = logger;
            
            BasePath = ResolveBasePath(config);

            Directory.CreateDirectory(TempPath);
            Directory.CreateDirectory(ConfigPath);
            Directory.CreateDirectory(DataPath);

            _logger.LogInformation("Using '{BasePath}' as Application BasePath (Version: {Version})", BasePath, CurrentVersion);

            // Cleanup temp directory from eventual remaining files
            _logger.LogInformation("Cleaning '{TempPath}' for application startup", TempPath);
            EmptyTemp();
        }

        public string ConfigPath => Path.Combine(BasePath, ConfigPathName);
        public string TempPath => Path.Combine(BasePath, TempPathName);
        public string DataPath => Path.Combine(BasePath, DataPathName);

        public string BasePath { get; }

        public Version CurrentVersion => GetCurrentVersion();

        /// <summary>
        /// Release all resources and delete temp files if the CleanupTempFileOnClose configuration flag is set to true.
        /// </summary>
        public void Dispose()
        {
            if (!_config.CleanupTempFileOnClose)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Emptying '{TempPath}'", TempPath);
                EmptyTemp();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to empty '{TempPath}'", TempPath);
            }
        }


        /// <summary>
        ///     Returns a FileName within the data folder.
        ///     The Directory is automatically created
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
        ///     Open a Temporary filestream. The file will be deleted on close.
        /// </summary>
        /// <returns>an open R/W FileStream</returns>
        public FileStream OpenTempFile()
        {
            return new FileStream(GetTempFileName(),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                4096, FileOptions.RandomAccess | FileOptions.DeleteOnClose);
        }

        /// <summary>
        ///     Return a Temporary filename. The path will point into the temp folder of BasePath.
        /// </summary>
        /// <returns>Temporary file path</returns>
        public string GetTempFileName()
        {
            return Path.Combine(TempPath, Path.GetRandomFileName());
        }

        /// <summary>
        ///     Delete the complete Temporary folder content, useful at launch to cleanup any remaining temporary files of
        ///     an eventual crash
        /// </summary>
        public void EmptyTemp()
        {
            new DirectoryInfo(TempPath).DeleteAllContent();
        }

        /// <summary>
        ///     Delete the complete Application folder content, including data and configuration. Use with care !
        /// </summary>
        public void EmptyAll()
        {
            new DirectoryInfo(BasePath).DeleteAllContent();
        }

        /// <summary>
        ///     Returns the configuration file names available for the given configuration, without instantiating
        ///     a full <see cref="AppData"/>. Used while building the host configuration.
        /// </summary>
        internal static IEnumerable<string> GetConfigFileNames(AppDataConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            var configPath = Path.Combine(ResolveBasePath(config), ConfigPathName);
            if (!Directory.Exists(configPath))
            {
                return [];
            }

            return Directory.GetFiles(configPath).OrderBy(x => x);
        }

        internal void SetCurrentVersion(Version version)
        {
            _logger.LogInformation("AppData version set to: {Version}", version);
            File.WriteAllText(GetCurrentVersionFileName(), version.ToString());
        }

        private Version GetCurrentVersion()
        {
            string versionFilePath = GetCurrentVersionFileName();

            if (!File.Exists(versionFilePath))
            {
                return new Version();
            }

            return Version.Parse(File.ReadAllText(versionFilePath));
        }

        private string GetCurrentVersionFileName()
        {
            return Path.Combine(BasePath, VersionFileName);
        }

        private static string ResolveBasePath(AppDataConfig config)
        {
            return config.BasePath ?? GetDefaultBasePath();
        }

        private static string GetDefaultBasePath()
        {
            var applicationName = Assembly.GetEntryAssembly()?.GetName().Name;
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new InvalidOperationException(
                    "Unable to determine the entry assembly name to build the default AppData BasePath. Configure an explicit 'BasePath' instead.");

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                    Environment.SpecialFolderOption.None),
                applicationName);
        }
    }
}