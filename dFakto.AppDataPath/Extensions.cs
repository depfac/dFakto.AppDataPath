using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace dFakto.AppDataPath
{
    public static class Extensions
    {
        private const string DATA_PATH_CONFIG = "DataPathConfig";

        public static IHostBuilder AddAppBasePath(this IHostBuilder hostBuilder, string sectionName)
        {
            hostBuilder.ConfigureAppConfiguration((x, y) =>
            {
                var appDataConfig = new AppDataPathConfig();
                x.Configuration.GetSection(sectionName).Bind(appDataConfig);
                var appData = new AppDataPath(null, appDataConfig);
                hostBuilder.Properties.Add(DATA_PATH_CONFIG, appDataConfig);
                foreach (var configFileName in appData.GetConfigFileNames())
                {
                    // Support other types of config ?
                    y.AddJsonFile(configFileName);
                }
            });
            hostBuilder.ConfigureServices((x, y) =>
            {
                y.AddSingleton((AppDataPathConfig) x.Properties[DATA_PATH_CONFIG]);
                y.AddSingleton<AppDataPath>();
            });
            return hostBuilder;
        }
    }
}