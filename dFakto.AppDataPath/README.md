# AppDataPath

Handle application data for dFakto.

`AppDataPath` gives an application a single, well-known root folder for its data, split into three
standard subfolders, plus a lightweight versioned-migration system for upgrading the contents of
that folder between application releases.

## Directory layout

Given a base path, `AppDataPath` exposes three standard subdirectories:

| Directory | Purpose                                                                    |
|-----------|----------------------------------------------------------------------------|
| `config/` | JSON files typically loaded as part of the host's configuration.           |
| `data/`   | Persistent application data. This is the directory affected by migrations. |
| `temp/`   | Temporary files. Purged automatically at application startup.              |

If no base path is configured, it defaults to:

```
<SpecialFolder.ApplicationData>/<entry assembly name>
```

e.g. `%AppData%\MyApp` on Windows or `~/.config/MyApp` on Linux.

> **Note:** the configured base path must be an absolute/rooted path. Relative paths are not supported.

## Installation

```bash
dotnet add package dFakto.AppDataPath
```

## Quick start with `IHostBuilder`

`AddAppData` on `IHostBuilder`:
1. Binds an `AppDataConfig` from the given configuration section (before any other configuration
   sources are added, so it can be set via environment variables, command line, etc.).
2. Loads every `*.json` file found in the `config/` directory (in alphabetical order) as an
   additional configuration source.
3. Registers `IAppData`, `IAppDataMigrator`, `IAppDataMigrationProvider` and the bound
   `AppDataConfig` into the service collection.

```csharp
using dFakto.AppDataPath;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .AddAppData("AppDataPath") // name of the configuration section to bind AppDataConfig from
    .ConfigureServices((_, services) =>
    {
        services.AddTransient<IAppDataMigration, MyFirstMigration>();
        services.AddHostedService<MyHostedService>();
    })
    .Build()
    .Run();
```

Configuration section example (`appsettings.json`):

```json
{
  "AppDataPath": {
    "BasePath": "/var/lib/myapp"
  }
}
```

`BasePath` is optional; omit it to use the default location described above.

### Running migrations

Migrations are **not** run automatically — call `IAppDataMigrator.Migrate()` explicitly, typically
at startup, before the rest of the application relies on `data/`:

```csharp
public class MyHostedService : IHostedService
{
    public MyHostedService(IAppDataMigrator appDataMigrator)
    {
        appDataMigrator.Migrate();
    }

    // ...
}
```

## Quick start without `IHostBuilder`

If you already have an `IServiceCollection` and want to configure `AppDataPath` manually:

```csharp
using dFakto.AppDataPath;

var config = new AppDataConfig { BasePath = "/var/lib/myapp" };

services.AddAppData(config);
```

An optional minimal allowed version can be passed to either `AddAppData` overload (see
[Minimal allowed version](#minimal-allowed-version) below):

```csharp
services.AddAppData(config, minimalAllowedVersion: new Version(2, 0));
```

## Using `IAppData`

`IAppData` is registered as a singleton and gives access to the base path and its standard
subdirectories, plus helpers for building paths and doing simple file I/O safely within them.

```csharp
public class MyService
{
    private readonly IAppData _appData;

    public MyService(IAppData appData)
    {
        _appData = appData;
    }

    public void Example()
    {
        // Well-known roots
        var basePath = _appData.BasePath;
        var dataPath = _appData.DataPath;

        // Build a path inside a standard directory (does not create anything)
        var settingsPath = _appData.GetFilePath(AppDataDir.Config, "settings.json");

        // Build a path and ensure its parent directories exist
        var logPath = _appData.GetFilePathAndCreateParents(AppDataDir.Data, "logs", "app.log");

        // Read/write helpers, scoped to a standard directory
        _appData.WriteAllText("hello", AppDataDir.Data, "greeting.txt");
        var content = _appData.ReadAllText(AppDataDir.Data, "greeting.txt");

        // List files
        var files = _appData.GetDirectoryFiles("*.json", SearchOption.AllDirectories, AppDataDir.Config);

        // Delete
        _appData.DeleteFile(AppDataDir.Data, "greeting.txt");
        _appData.DeleteDirectory(excludeTopDir: true, force: true, AppDataDir.Temp);
    }
}
```

Every path-producing method rejects segments that would resolve outside of `BasePath` (e.g. via
`..`) by throwing an `ArgumentException`.

## Writing migrations

A migration is a versioned unit of work applied to `data/`. Implement `IAppDataMigration` and
register it in the service collection; migrations run in ascending order of `Version`, and only
those with a `Version` greater than the current AppData version are applied:

```csharp
using dFakto.AppDataPath;

public class AddDefaultSettingsMigration : IAppDataMigration
{
    public Version Version => new Version(1, 0);

    public void Upgrade(IAppData appData, IServiceProvider serviceProvider)
    {
        appData.WriteAllText("{}", AppDataDir.Data, "settings.json");
    }
}
```

```csharp
services.AddTransient<IAppDataMigration, AddDefaultSettingsMigration>();
```

When `IAppDataMigrator.Migrate()` runs and one or more pending migrations are found:

1. The current contents of `data/` are backed up to a zip file.
2. Each pending migration's `Upgrade` is invoked, in version order.
3. If all migrations succeed, the AppData version is updated to the last applied migration's
   version, and the backup is discarded.
4. If any migration throws, `data/` is left as-is (partially migrated) and `Migrate()` rethrows as
   an `InvalidOperationException`. **The backup is not restored during this call.**

Recovery from a failed or interrupted migration always happens on the *next* call to `Migrate()`,
not within the call that failed — this also covers the case where the process crashes outright
mid-migration. On that next call, `Migrate()` detects the incomplete upgrade first and restores
`data/` from the backup (and rolls the AppData version back accordingly) before evaluating any
pending migrations. In practice this means: if `Migrate()` throws, restart the application (or call
`Migrate()` again) to complete the rollback before relying on the contents of `data/`.

Registering two migrations with the same `Version` is invalid and causes `Migrate()` to throw.

### Minimal allowed version

Both `AddAppData` overloads accept an optional `minimalAllowedVersion`. If the current AppData
version is older than `minimalAllowedVersion` (and this isn't a brand-new installation),
`Migrate()` throws instead of attempting to migrate — use this to explicitly refuse to upgrade
data that is too old for your migrations to handle correctly.
