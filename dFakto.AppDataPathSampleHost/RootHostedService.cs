using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using dFakto.AppDataPath;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace dFakto.AppDataPathSampleHost
{
    public class Mi1 : IAppDataMigration
    {
        public Version Version => new("1.0");

        public void Upgrade(AppData appData, IServiceProvider serviceProvider)
        {
            File.WriteAllText(appData.GetDataFileName("test2.txt"), "CONTENT");
        }
    }

    public class Mi2 : IAppDataMigration
    {
        public Version Version => new("2.0");

        public void Upgrade(AppData appData, IServiceProvider serviceProvider)
        {
            File.Delete(appData.GetDataFileName("test.txt"));
        }
    }

    public class RootHostedService : IHostedService
    {
        private readonly AppData _appData;
        private readonly ILogger<RootHostedService> _logger;

        public RootHostedService(
            AppData appData,
            AppDataMigrator appDataMigrator,
            ILogger<RootHostedService> logger,
            IHostApplicationLifetime appLifetime)
        {
            _appData = appData;
            _logger = logger;

            appDataMigrator.Migrate();

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("1. StartAsync has been called");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("4. StopAsync has been called");

            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("2. OnStarted has been called");
        }

        private void OnStopping()
        {
            _logger.LogInformation("3. OnStopping has been called");
        }

        private void OnStopped()
        {
            _logger.LogInformation("5. OnStopped has been called");
        }
    }
}