using dFakto.AppDataPath;
using dFakto.AppDataPathSampleHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = Host.CreateApplicationBuilder(args);
builder.AddAppData();
builder.Services.AddTransient<IAppDataMigration, Mi1>();
builder.Services.AddTransient<IAppDataMigration, Mi2>();
builder.Services.AddHostedService<RootHostedService>();

var host = builder.Build();
host.Run();
    