using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace dFakto.AppDataPath;

/// <summary>
/// Extensions for Microsoft.Extensions
/// </summary>
public static class Extensions
{
    private const string PropertiesKey = "AppDataPathConfig";

    /// <summary>
    /// Add required services to use AppData as part of an app that uses dependency injection.
    /// </summary>
    /// <param name="services">The service collection being built.</param>
    /// <param name="config">The configuration of the appdata instance.</param>
    /// <param name="minimalAllowedVersion">The minimal allowed version of the application data. If application data
    /// exists and its version is lower than this, any migration will fail.</param>
    public static IServiceCollection AddAppData(this IServiceCollection services, AppDataConfig config,
        Version? minimalAllowedVersion = null)
    {
        services.AddSingleton(config);
        services.AddSingleton<IAppDataMigrator, AppDataMigrator>(serviceProvider =>
            new AppDataMigrator(serviceProvider, minimalAllowedVersion));
        services.AddSingleton<IAppDataMigrationProvider, DefaultAppDataMigrationProvider>();
        services.AddSingleton<IAppData, AppData>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<AppData>>();
            var newAppData = new AppData(serviceProvider.GetRequiredService<AppDataConfig>());
            logger?.LogInformation("Using '{BasePath}' as Application BasePath. (Version : {CurrentVersion})",
                newAppData.BasePath, newAppData.CurrentVersion);

            try
            {
                // Attempt cleaning up the temp directory.
                newAppData.DeleteDirectory(true, true, AppDataDir.Temp);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to empty AppData temporary folder");
            }

            return newAppData;
        });

        return services;
    }

    /// <summary>
    /// Add required services to use AppData as part of an application host. The configuration of the AppData instance
    /// is retrieved from default configuration entries in the given config section.
    ///
    /// If any JSON configuration files are available it the ConfigPath of AppData, these will be loaded here.
    ///
    /// Typically, one would want to call IAppDataMigrator.Migrate() once the host has finished building,
    /// before running it.
    /// </summary>
    /// <param name="hostBuilder">The host being built.</param>
    /// <param name="configSectionName">The name of the configuration section that will be used to retrieve the
    /// AppDataConfig</param>
    /// <param name="minimalAllowedVersion">The minimal allowed version of the application data. If application data
    /// exists and its version is lower than this, any migration will fail.</param>
    public static IHostBuilder AddAppData(this IHostBuilder hostBuilder, string configSectionName,
        Version? minimalAllowedVersion = null)
    {
        hostBuilder.ConfigureAppConfiguration((x, y) =>
        {
            var appDataConfig = new AppDataConfig();
            x.Configuration.GetSection(configSectionName).Bind(appDataConfig);
            var appData = new AppData(appDataConfig);
            hostBuilder.Properties[PropertiesKey] = appDataConfig;

            foreach (var configFileName in appData
                         .GetDirectoryFiles("*.json", SearchOption.AllDirectories, AppDataDir.Config)
                         .OrderBy(path => path))
            {
                y.AddJsonFile(configFileName);
            }
        });
        hostBuilder.ConfigureServices((x, y) =>
        {
            y.AddAppData((AppDataConfig) x.Properties[PropertiesKey], minimalAllowedVersion);
        });
        return hostBuilder;
    }
}