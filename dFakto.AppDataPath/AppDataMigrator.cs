using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dFakto.AppDataPath
{
    /// <summary>
    /// Default implementation of the IAppDataMigrator
    /// </summary>
    internal class AppDataMigrator : IAppDataMigrator
    {
        private const string UpgradeFileName = "UPGRADING.txt";
        private const string BackupFileName = "APPDATA_BACKUP.zip";

        private readonly AppData _appData;
        private readonly string _backupFilePath;
        private readonly ILogger<AppDataMigrator> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Version? _minimalAllowedVersion;
        private readonly string _upgradeVersionFilePath;

        public AppDataMigrator(IServiceProvider serviceProvider, Version? minimalAllowedVersion = null)
        {
            _serviceProvider = serviceProvider;
            _minimalAllowedVersion = minimalAllowedVersion;
            _logger = _serviceProvider.GetService<ILogger<AppDataMigrator>>();
            _appData = _serviceProvider.GetService<AppData>();
            _backupFilePath = Path.Combine(_appData.BasePath, BackupFileName);
            _upgradeVersionFilePath = Path.Combine(_appData.BasePath, UpgradeFileName);
        }

        private bool MigrationAborted => File.Exists(_upgradeVersionFilePath);

        public void Migrate()
        {
            var currentVersion = _appData.CurrentVersion;

            // If a minimal version cutoff is defined, and this isn't a new installation, and the current version
            // is lower than the minimal allowed version, then we should abort the migration. We cannot handle
            // application data that is this old.
            if (_minimalAllowedVersion != null &&
                currentVersion != new Version() &&
                currentVersion < _minimalAllowedVersion)
            {
                throw new InvalidOperationException(
                    $"Current AppData version \"{currentVersion}\" is lower than minimal allowed version " +
                    $"\"{_minimalAllowedVersion}\" to execute an upgrade migration. Aborting.");
            }

            if (MigrationAborted)
            {
                // If an upgrade has already been attempted, then we are probably recovering from a crash,
                // so run Restore procedures before trying to upgrade or running the app.
                _logger.LogWarning("Metavault AppDataPath upgrade detected a crash during update. Recovering");
                Restore();
                _logger.LogInformation("Metavault AppDataPath upgrade recovery complete");
            }

            var migrations = _serviceProvider.GetService<IAppDataMigrationProvider>().GetAppDataMigration().ToList();

            CheckDuplicates(migrations);

            migrations = migrations.Where(x => x.Version > currentVersion).ToList();

            if (migrations.Count == 0)
            {
                _logger.LogDebug("AppData is already at the latest version");
            }
            else
            {
                migrations.Sort((x, y) => x.Version.CompareTo(y.Version));

                _logger.LogInformation("{Count} Migrations of AppData must be performed, creating Backup first",
                    migrations.Count);
                // We need to upgrade the application. Make a backup
                Backup();
                _logger.LogDebug("Backup completed");

                try
                {
                    SaveOldVersion(currentVersion);

                    Version latestVersion = currentVersion;
                    // Apply the actual upgrades
                    foreach (var migration in migrations)
                    {
                        _logger.LogInformation("Upgrading to version {Version}", migration.Version);
                        migration.Upgrade(_appData, _serviceProvider);
                        latestVersion = migration.Version;
                    }

                    _appData.SetCurrentVersion(latestVersion);

                    _logger.LogInformation("Migration completed, cleaning up");
                    File.Delete(_upgradeVersionFilePath);
                    File.Delete(_backupFilePath);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while applying migrations, restoring backup");
                    Restore();
                    _logger.LogInformation("Backup restored successfully");
                    throw;
                }
            }
        }

        private static void CheckDuplicates(List<IAppDataMigration> migrations)
        {
            var duplicates = migrations.GroupBy(x => x.Version)
                .Where(g => g.Count() > 1)
                .ToDictionary(x => x.Key, y => y.Count());

            if (duplicates.Count > 0)
            {
                var (version, value) = duplicates.First();
                throw new Exception($"{value} migrations are targeting the version {version}");
            }
        }

        private void Restore()
        {
            new DirectoryInfo(_appData.DataPath).DeleteAllContent();
            ZipFile.ExtractToDirectory(_backupFilePath, _appData.DataPath, true);

            _appData.SetCurrentVersion(RetrieveOldVersion());

            File.Delete(_upgradeVersionFilePath);
            File.Delete(_backupFilePath);
        }

        private void Backup()
        {
            if (File.Exists(_backupFilePath))
            {
                File.Delete(_backupFilePath);
            }

            ZipFile.CreateFromDirectory(_appData.DataPath, _backupFilePath);
        }

        private Version RetrieveOldVersion()
        {
            return Version.Parse(File.ReadAllText(_upgradeVersionFilePath));
        }

        private void SaveOldVersion(Version version)
        {
            File.WriteAllText(_upgradeVersionFilePath, version.ToString());
        }
    }
}