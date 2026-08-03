using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dFakto.AppDataPath;

internal class AppDataMigrator : IAppDataMigrator
{
    private const string UpgradeFileName = "UPGRADING.txt";
    private const string BackupFileName = "APPDATA_BACKUP.zip";

    private readonly IAppData _appData;
    private readonly ILogger<AppDataMigrator>? _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Version? _minimalAllowedVersion;

    public AppDataMigrator(IServiceProvider serviceProvider, Version? minimalAllowedVersion = null)
    {
        _serviceProvider = serviceProvider;
        _minimalAllowedVersion = minimalAllowedVersion;
        _appData = _serviceProvider.GetRequiredService<IAppData>();
        _logger = _serviceProvider.GetService<ILogger<AppDataMigrator>>();
    }

    private bool MigrationAborted => File.Exists(_appData.GetFilePath(UpgradeFileName));

    public void Migrate()
    {
        if (MigrationAborted)
        {
            // If an upgrade has already been attempted, then we are probably recovering from a crash,
            // so run Restore procedures before trying to upgrade or running the app.
            _logger?.LogWarning("AppDataPath upgrade detected a crash during update. Recovering");
            Restore();
            _logger?.LogInformation("AppDataPath upgrade recovery complete");
        }

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

        var migrations = _serviceProvider.GetService<IAppDataMigrationProvider>()?.GetAppDataMigrations().ToList() ??
                         new List<IAppDataMigration>();

        CheckDuplicates(migrations);

        migrations = migrations.Where(x => x.Version > currentVersion).ToList();

        if (migrations.Count == 0)
        {
            _logger?.LogDebug("AppData is already at the latest version");
        }
        else
        {
            migrations.Sort((x, y) => x.Version.CompareTo(y.Version));

            _logger?.LogInformation("{Count} Migrations of AppData must be performed, creating Backup first",
                migrations.Count);
            // We need to upgrade the application. Make a backup
            Backup();
            _logger?.LogDebug("Backup completed");

            try
            {
                SaveOldVersion(currentVersion);

                Version latestVersion = currentVersion;
                // Apply the actual upgrades
                foreach (var migration in migrations)
                {
                    _logger?.LogInformation("Upgrading to version {Version}", migration.Version);
                    migration.Upgrade(_appData, _serviceProvider);
                    latestVersion = migration.Version;
                }

                SetVersion(latestVersion);

                _logger?.LogInformation("Migration completed, cleaning up");
                _appData.DeleteFile(UpgradeFileName);
                _appData.DeleteFile(BackupFileName);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    "Migration failed. Leaving directory as-is, AppDataPath will cleanup on restart", e);
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
            var firstDuplicate = duplicates.First();
            var version = firstDuplicate.Key;
            var count = firstDuplicate.Value;
            throw new InvalidOperationException($"{count} migrations are targeting the version {version}");
        }
    }

    private void Restore()
    {
        _appData.DeleteDirectory(true, true, AppDataDir.Data);

        var backupFilePath = _appData.GetFilePath(BackupFileName);
        var upgradeFilePath = _appData.GetFilePath(UpgradeFileName);
        if (File.Exists(backupFilePath))
            ZipFile.ExtractToDirectory(backupFilePath, _appData.DataPath);

        SetVersion(RetrieveOldVersion());

        if (File.Exists(upgradeFilePath))
            File.Delete(upgradeFilePath);
        if (File.Exists(backupFilePath))
            File.Delete(backupFilePath);
    }

    private void Backup()
    {
        var backupFilePath = _appData.GetFilePath(BackupFileName);
        if (File.Exists(backupFilePath))
        {
            File.Delete(backupFilePath);
        }

        if (Directory.Exists(_appData.DataPath))
            ZipFile.CreateFromDirectory(_appData.DataPath, backupFilePath);
    }

    private Version RetrieveOldVersion()
    {
        return Version.Parse(_appData.ReadAllText(UpgradeFileName));
    }

    private void SaveOldVersion(Version version)
    {
        _appData.WriteAllText(version.ToString(), UpgradeFileName);
    }

    private void SetVersion(Version version)
    {
        _appData.WriteAllText(version.ToString(), AppData.VersionFileName);
        _logger?.LogInformation("AppData version set to: {Version}", version);
    }
}