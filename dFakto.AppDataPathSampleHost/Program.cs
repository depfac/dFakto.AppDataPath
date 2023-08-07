using dFakto.AppDataPath;
using dFakto.AppDataPathSampleHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var host = Host.CreateDefaultBuilder(args)
    .AddAppData()
    .ConfigureServices((_, services) =>
    {
        services.AddTransient<IAppDataMigration, Mi1>();
        services.AddTransient<IAppDataMigration, Mi2>();
        services.AddHostedService<RootHostedService>();
    }).Build();
    
host.Run();
    