using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace dFakto.AppDataPath
{
    public static class Extensions
    {
        private const string AppDataConfig = "AppDataPathConfig";

        public static IHostBuilder AddAppData(this IHostBuilder hostBuilder, string sectionName)
        {
            hostBuilder.ConfigureAppConfiguration((x, y) =>
            {
                var appDataConfig = new AppDataConfig();
                x.Configuration.GetSection(sectionName).Bind(appDataConfig);
                var appData = new AppData(null, appDataConfig, sectionName);
                hostBuilder.Properties.Add(AppDataConfig, appDataConfig);
                foreach (var configFileName in appData.GetConfigFileNames())
                {
                    // Support other types of config ?
                    y.AddJsonFile(configFileName);
                }
            });
            hostBuilder.ConfigureServices((x, y) =>
            {
                y.AddSingleton((AppDataConfig) x.Properties[AppDataConfig]);
                y.AddSingleton<AppData>();
            });
            return hostBuilder;
        }
    }
}