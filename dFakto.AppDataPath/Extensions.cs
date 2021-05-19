using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace dFakto.AppDataPath
{
    public static class Extensions
    {
        private const string AppDataConfig = "AppDataPathConfig";

        public static IServiceCollection AddAppData(this IServiceCollection services, AppDataConfig config)
        {
            services.AddSingleton(config);
            services.AddSingleton<AppDataMigrator>();
            services.AddSingleton<IAppDataMigrationProvider, DefaultAppDataMigrationProvider>();
            services.AddSingleton<AppData>();
            return services;
        }
        
        public static IHostBuilder AddAppData(this IHostBuilder hostBuilder, string sectionName)
        {
            hostBuilder.ConfigureAppConfiguration((x, y) =>
            {
                var appDataConfig = new AppDataConfig();
                x.Configuration.GetSection(sectionName).Bind(appDataConfig);
                var appData = new AppData(null, appDataConfig);
                hostBuilder.Properties.Add(AppDataConfig, appDataConfig);
                foreach (var configFileName in appData.GetConfigFileNames())
                {
                    // Support other types of config ?
                    y.AddJsonFile(configFileName);
                }
            });
            hostBuilder.ConfigureServices((x, y) =>
            {
                y.AddAppData((AppDataConfig) x.Properties[AppDataConfig]);
            });
            return hostBuilder;
        }

        /// <summary>
        ///     Delete the contents of a directory. This does not delete the directory itself.
        /// </summary>
        /// <param name="directoryInfo">The directory to empty</param>
        public static void DeleteAllContent(this DirectoryInfo directoryInfo)
        {
            SetAttributesNormal(directoryInfo);

            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (var directory in directoryInfo.GetDirectories())
            {
                directory.Delete(true);
            }
        }

        /// <summary>
        ///     Set the attributes of the complete content of a directory to Normal, i.e not ReadOnly
        /// </summary>
        /// <param name="dir">Path to the directory</param>
        private static void SetAttributesNormal(DirectoryInfo di)
        {
            foreach (var subDir in di.GetDirectories())
            {
                SetAttributesNormal(subDir);
            }

            foreach (var file in di.GetFiles())
            {
                File.SetAttributes(file.FullName, FileAttributes.Normal);
            }
        }
    }
}