using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace dFakto.AppDataPath;

public static class Extensions
{
    private const string PropertiesKey = "AppDataPathConfig";

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