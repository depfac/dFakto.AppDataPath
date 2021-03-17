using dFakto.AppDataPath;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace dFakto.AppDataPathSampleHost
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .AddAppData("section")
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IAppDataMigration, Mi1>();
                    services.AddTransient<IAppDataMigration, Mi2>();
                    services.AddHostedService<RootHostedService>();
                });
        }
    }
}