using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace dFakto.AppDataPath
{
    public class AppDataPath : IDisposable
    {
        private const string CONFIG_PATH_NAME = "config";
        private const string TEMP_PATH_NAME = "temp";
        private const string DATA_PATH_NAME = "data";

        private readonly ILogger<AppDataPath> _logger;
        private readonly AppDataPathConfig _config;
        private readonly string _basePath;
        public string ConfigPath => Path.Combine(_basePath, CONFIG_PATH_NAME);
        public string TempPath => Path.Combine(_basePath, TEMP_PATH_NAME);
        public string DataPath => Path.Combine(_basePath, DATA_PATH_NAME);

        public AppDataPath(ILogger<AppDataPath> logger, AppDataPathConfig config)
        {
            _config = config ?? throw new ArgumentException(nameof(config));
            _basePath = config.BasePath;
            _logger = logger;
            if (string.IsNullOrEmpty(_basePath))
            {
                _basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                    Environment.SpecialFolderOption.None);
            }

            Directory.CreateDirectory(TempPath);
            Directory.CreateDirectory(ConfigPath);
            Directory.CreateDirectory(DataPath);

            // Logger may be null when loading configuration
            _logger?.LogInformation($"Using '{_basePath}' as Application BasePath");

            // Cleanup temp directory from eventual remaining files
            _logger?.LogInformation($"Cleaning '{TempPath}' for application startup");
            EmptyTemp();
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
        /// Delete the contents of a directory. This does not delete the directory itself.
        /// </summary>
        /// <param name="path">The path to the directory</param>
        private void EmptyDirectory(string path)
        {
            var di = new DirectoryInfo(path);

            SetAttributesNormal(path);
            foreach (var file in di.GetFiles())
                file.Delete();
            foreach (var directory in di.GetDirectories())
                directory.Delete(true);
        }

        /// <summary>
        /// Delete the complete Temporary folder content, useful at launch to cleanup any remaining temporary files of
        /// an eventual crash
        /// </summary>
        public void EmptyTemp()
        {
            EmptyDirectory(TempPath);
        }

        /// <summary>
        /// Delete the complete Application folder content, including data and configuration. Use with care !
        /// </summary>
        public void EmptyAll()
        {
            EmptyDirectory(_basePath);
        }

        /// <summary>
        /// Set the attributes of the complete content of a directory to Normal, i.e not ReadOnly
        /// </summary>
        /// <param name="dir">Path to the directory</param>
        private static void SetAttributesNormal(string dir)
        {
            var di = new DirectoryInfo(dir);

            foreach (var subDir in di.GetDirectories())
                SetAttributesNormal(subDir.FullName);

            foreach (var file in di.GetFiles())
                File.SetAttributes(file.FullName, FileAttributes.Normal);
        }

        public IEnumerable<string> GetConfigFileNames()
        {
            return Directory.GetFiles(ConfigPath);
        }

        public void Dispose()
        {
            if (_config.CleanupTempFileOnClose)
            {
                try
                {
                    _logger?.LogInformation($"Emptying '{TempPath}'");
                    EmptyTemp();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Unable to empty '{TempPath}'");
                }
            }
        }
    }
}
