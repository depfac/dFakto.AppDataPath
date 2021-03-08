using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dFakto.AppDataPath
{
    public class AppDataMigrator
    {
        private readonly IServiceProvider _serviceProvider;
        private const string VersionFileName = "VERSION.txt";
        private const string UpgradeFileName = "UPGRADING.txt";
        private const string BackupFileName = "APPDATA_BACKUP.zip";
        
        private readonly AppData _appData;
        private readonly string _backupFilePath;
        private readonly string _upgradeVersionFilePath;
        private readonly string _versionFilePath;
        private readonly ILogger<AppDataMigrator> _logger;

        public AppDataMigrator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetService<ILogger<AppDataMigrator>>();
            _appData = _serviceProvider.GetService<AppData>();
            _backupFilePath = Path.Combine(_appData.BasePath, BackupFileName);
            _upgradeVersionFilePath = Path.Combine(_appData.BasePath, UpgradeFileName);
            _versionFilePath = Path.Combine(_appData.BasePath, VersionFileName);
        }
        
        public void Migrate()
        {
            // If this is a new install, or if the VERSION.txt does not exist for some reason
            // (pre-versioning deployment) tag this as pre-oldest version.
            // The oldest version is responsible for first deployment and migration of legacy
            // appdatapaths.
            var currentVersion = GetVersion(_versionFilePath);
            var upgradingVersion = GetVersion(_upgradeVersionFilePath);
            if (upgradingVersion != null)
            {
                // If an upgrade has already been attempted, then we are probably recovering from a crash,
                // so run Restore procedures before trying to upgrade or running the app.
                _logger.LogWarning("Metavault AppDataPath upgrade detected a crash during update. Recovering");
                Restore();
                _logger.LogInformation("Metavault AppDataPath upgrade recovery complete");
            }

            var migrations = _serviceProvider.GetServices<IAppDataMigration>()
                .Where(x => x.Version > currentVersion)
                .ToList();

            if (migrations.Count == 0)
            {
                _logger.LogDebug("AppData is already at the latest version");
            }
            else
            {
                migrations.Sort((x, y) => x.Version.CompareTo(y.Version));

                _logger.LogInformation("{Count} Migrations of AppData must be performed, creating Backup first", migrations.Count);
                // We need to upgrade the application. Make a backup
                Backup();
                _logger.LogDebug("Backup completed");
                
                try
                {
                    if (currentVersion != null)
                    {
                        SetVersion(_upgradeVersionFilePath, currentVersion);
                    }
                    
                    // Apply the actual upgrades
                    foreach (var migration in migrations)
                    {
                        _logger.LogInformation("Upgrading to version {Version}",migration.Version);
                        migration.Upgrade(_appData, _serviceProvider);

                        SetVersion(_versionFilePath, migration.Version);
                    }

                    _logger.LogInformation("Migration completed, cleaning up");
                    File.Delete(_upgradeVersionFilePath);
                    File.Delete(_backupFilePath);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,"Error while applying migrations, restoring backup");
                    Restore();
                    _logger.LogInformation("Backup restored successfully");
                    throw;
                }

            }

        }
        
        private void Restore()
        {
            new DirectoryInfo(_appData.DataPath).DeleteAllContent();
            ZipFile.ExtractToDirectory(_backupFilePath, _appData.DataPath, true);

            var previous = GetVersion(_upgradeVersionFilePath);
            if (previous != null)
            {
                SetVersion(_versionFilePath, previous);
            }
            else
            {
                File.Delete(_versionFilePath);
            }

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

        private static Version? GetVersion(string versionFilePath)
        {
            if (!File.Exists(versionFilePath))
                return null;
            return Version.Parse(File.ReadAllText(versionFilePath));
        }

        private static void SetVersion(string versionFilePath, Version version)
        {
            File.WriteAllText(versionFilePath, version.ToString());
        }
    }
}