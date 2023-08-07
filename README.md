# dFakto.AppDataPath

The goal of this project is to manage all the files that a project needs in a single directory structure.

## Folder Structure

The folder structure under the ```basePath``` (see below) looks like this :

- ```tmp/```: Temporary files that should be discarded when no longer needed. It's important to regularly clean up these files.
- ```config/```: JSON files that need to be integrated into the global Configuration system.
- ```data/```: Any file used by the application that needs to be persisted, with a format that may evolve over time (see Migrations below).
- ```VERSION.txt```: A simple file that contains the version of the structure. Refer to the section below for Migrations.

Storing all these files in the same subdirectory greatly simplifies management, especially when the application is deployed with Docker. A single volume can be utilized for all purposes.

## Usage

The simplest way to integrate AppData is to call `AddAppData()` on the `HostBuilder`.

```csharp
var host = Host.CreateDefaultBuilder(args)
    .AddAppData()
    .Build();
host.Run();
```

### Configuration

Two configuration key can be used
 - ```BasePath``` : Specify the base directory to use. The default value is ```ApplicationData Folder/Assembly name/```.
 - ```CleanupTempFileOnClose``` : Specify if the temp directory mus be emptied when Disposing the AppData. The default value is TRUE

The default section name is "AppDataPath" and can be customized by using the optional parameter ```AddAppData("MyCustomSectionName")```.

#### Using Environment Variable
```shell
export DOTNET_APPDATAPATH_BASEPATH="/tmp/test"
```
Note : Environment variable configuration must be enabled using [AddEnvironmentVariables](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.environmentvariablesextensions.addenvironmentvariables?view=dotnet-plat-ext-7.0#microsoft-extensions-configuration-environmentvariablesextensions-addenvironmentvariables(microsoft-extensions-configuration-iconfigurationbuilder-system-string))

#### Use appsettings.json
```json 
{
  "AppDataPath": {
    "BasePath": "/tmp/test",
    "CleanupTempFileOnClose": false
  }
}
```

It's therefore possible to use Environment Variable or configuration file to Set these configuration keys.

## Migrations

dFakto.AppDataPath includes an integrated migration process designed to facilitate the transition of file formats between different versions of the application.

The migration process is designed with a high level of caution. Prior to performing any migration, a complete backup of the folder is created. This precaution ensures that in the event of an error during migration, the folder can be automatically restored to its original state.

## Usage

The migration process is carried out in two steps:

1. Registering migration processes
2. Executing migrations during startup

### Registering Migrations

Each migration is represented by a class that implements the `IAppDataMigration` interface. All migrations must be added to the dependency injection.

#### Example of Migration Registrations

```csharp
var host = Host.CreateDefaultBuilder(args)
    .AddAppData()
    .ConfigureServices((_, services) =>
    {
        services.AddTransient<IAppDataMigration, Mi1>();
        services.AddTransient<IAppDataMigration, Mi2>();
    }).Build();
host.Run();
```

The order of registration for migrations holds no significance. The migrations will be executed based on the `Version` property of each migration class.

### Executing Migrations During Startup

Retrieve the `IAppDataMigrator` instance from the dependency injection and call the `Migrate` method.

Note: Depending on the specific migration being executed, the `Migrate` method call may take some time to complete.

```csharp
var migrator = services.GetRequiredService<IAppDataMigrator>(); // Retrieve the IAppDataMigrator from DI
await migrator.Migrate();
```