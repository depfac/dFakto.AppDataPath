using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace dFakto.AppDataPath.Tests;

public class AppDataPathMigrationTests : IDisposable
{
    private readonly AppDataConfig _appDataConfig = new ();

    public AppDataPathMigrationTests()
    {
        var tmp = Path.GetTempFileName();
        File.Delete(tmp);
        _appDataConfig.BasePath = tmp;
    }

    public void Dispose()
    {
        if (!string.IsNullOrEmpty(_appDataConfig.BasePath) && Directory.Exists(_appDataConfig.BasePath))
            Directory.Delete(_appDataConfig.BasePath, true);
    }

    private IServiceProvider ConfigureServiceProvider(IEnumerable<IAppDataMigration> migrations)
    {
        var y = new ServiceCollection();

        y.AddLogging();
        y.AddAppData(_appDataConfig);

        foreach (var mig in migrations)
        {
            y.AddSingleton(mig);
        }

        return y.BuildServiceProvider();
    }

    [Fact]
    public void TestNoMigration()
    {
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[0]);

        serviceProvider.GetService<IAppDataMigrator>().Migrate();

        Assert.Equal(new Version(), serviceProvider.GetService<IAppData>().CurrentVersion);
    }

    [Fact]
    public void TestCreateSingleFileMigration()
    {
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new OverwriteFileMigration(
                new Version(1, 0),
                "test.txt",
                "test")
        });

        serviceProvider.GetService<IAppDataMigrator>().Migrate();

        var appData = serviceProvider.GetService<IAppData>();

        Assert.Equal(new Version(1, 0), appData.CurrentVersion);
        Assert.True(File.Exists(appData.GetFilePathAndCreateParents(AppDataDir.Data, "test.txt")));
        Assert.Equal("test", appData.ReadAllText(AppDataDir.Data, "test.txt"));
    }

    [Fact]
    public void TestCreateTwoFilesMigration()
    {
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new OverwriteFileMigration(
                new Version(1, 0),
                "test.txt",
                "test"),

            new OverwriteFileMigration(
                new Version(1, 1),
                "test2.txt",
                "test2")
        });

        serviceProvider.GetService<IAppDataMigrator>().Migrate();

        var appData = serviceProvider.GetService<IAppData>();

        Assert.Equal(new Version(1, 1), appData.CurrentVersion);
        Assert.True(File.Exists(appData.GetFilePath(AppDataDir.Data, "test.txt")));
        Assert.Equal("test", appData.ReadAllText(AppDataDir.Data, "test.txt"));
        Assert.True(File.Exists(appData.GetFilePath(AppDataDir.Data, "test2.txt")));
        Assert.Equal("test2", appData.ReadAllText(AppDataDir.Data, "test2.txt"));
    }

    [Fact]
    public void TestCreateTwoFilesMigrationUnordered()
    {
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            //Put version 2.0 first
            new OverwriteFileMigration(
                new Version(2, 0),
                "test.txt",
                "replaced"),

            new OverwriteFileMigration(
                new Version(1, 1),
                "test.txt",
                "will be overwritten")
        });

        serviceProvider.GetService<IAppDataMigrator>().Migrate();

        var appData = serviceProvider.GetService<IAppData>();

        Assert.Equal(new Version(2, 0), appData.CurrentVersion);
        Assert.True(File.Exists(appData.GetFilePath(AppDataDir.Data, "test.txt")));
        Assert.Equal("replaced", appData.ReadAllText(AppDataDir.Data, "test.txt"));
    }

    [Fact]
    public void TestCreateTwoFilesMigrationInTwoSteps()
    {
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new OverwriteFileMigration(
                new Version(1, 0),
                "test.txt",
                "test")
        });

        serviceProvider.GetService<IAppDataMigrator>().Migrate();

        serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new OverwriteFileMigration(
                new Version(1, 1),
                "test2.txt",
                "test2")
        });

        serviceProvider.GetService<IAppDataMigrator>().Migrate();

        var appData = serviceProvider.GetService<IAppData>();

        Assert.Equal(new Version(1, 1), appData.CurrentVersion);
        Assert.True(File.Exists(appData.GetFilePath(AppDataDir.Data, "test.txt")));
        Assert.Equal("test", appData.ReadAllText(AppDataDir.Data, "test.txt"));
        Assert.True(File.Exists(appData.GetFilePath(AppDataDir.Data, "test2.txt")));
        Assert.Equal("test2", appData.ReadAllText(AppDataDir.Data, "test2.txt"));
    }

    [Fact]
    public void TestErrorInFirstMigration()
    {
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new ThrowExceptionMigration<IOException>(new Version(1, 0))
        });

        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate());

        var appData = serviceProvider.GetService<IAppData>();

        Assert.Equal(new Version(), appData.CurrentVersion);
    }

    [Fact]
    public void TestErrorRollback()
    {
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new OverwriteFileMigration(
                new Version(1, 0),
                "test.txt",
                "test"),
            new ThrowExceptionMigration<IOException>(new Version(1, 1))
        });

        // Crash
        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate());

        // Rollback
        var newServiceProvider = ConfigureServiceProvider([]);
        var appData = newServiceProvider.GetService<IAppData>();
        newServiceProvider.GetService<IAppDataMigrator>().Migrate();

        // Verify directory remains in original state
        Assert.Equal(new Version(), appData.CurrentVersion);
        Assert.False(File.Exists(appData.GetFilePath(AppDataDir.Data, "test.txt")));
    }

    [Fact]
    public void TestErrorRollbackWithPreviousMigration()
    {
        // Succeed migration
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new OverwriteFileMigration(
                new Version(1, 0),
                "test.txt",
                "original")
        });

        serviceProvider.GetService<IAppDataMigrator>().Migrate();

        // Crash new migration
        serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new OverwriteFileMigration(
                new Version(2, 1),
                "test.txt",
                "changed !"),
            new ThrowExceptionMigration<IOException>(new Version(2, 2))
        });

        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate());

        // Rollback on restart
        serviceProvider = ConfigureServiceProvider([]);
        serviceProvider.GetService<IAppDataMigrator>().Migrate();

        var appData = serviceProvider.GetService<IAppData>();

        Assert.Equal(new Version(1, 0), appData.CurrentVersion);
        Assert.True(File.Exists(appData.GetFilePath(AppDataDir.Data, "test.txt")));
        Assert.Equal("original", appData.ReadAllText(AppDataDir.Data, "test.txt"));
    }

    [Fact]
    public void TestDuplicateMigrationForSameVersionThrowsException()
    {
        var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
        {
            new OverwriteFileMigration(new Version(1, 0), "test.txt", "original"),
            new OverwriteFileMigration(new Version(1, 0), "test2.txt", "coucou")
        });

        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate());
    }

    public class OverwriteFileMigration : IAppDataMigration
    {
        private readonly string _content;
        private readonly string _fileName;

        public OverwriteFileMigration(Version version, string fileName, string content)
        {
            _fileName = fileName;
            _content = content;
            Version = version;
        }

        public Version Version { get; }

        public void Upgrade(IAppData appData, IServiceProvider serviceProvider)
        {
            appData.WriteAllText(_content, AppDataDir.Data, _fileName);
        }
    }

    public class ThrowExceptionMigration<T> : IAppDataMigration where T : Exception, new()
    {
        public ThrowExceptionMigration(Version version)
        {
            Version = version;
        }

        public Version Version { get; }

        public void Upgrade(IAppData appData, IServiceProvider serviceProvider)
        {
            throw new T();
        }
    }
}