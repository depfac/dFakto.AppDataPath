using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace dFakto.AppDataPath.Tests
{
    public class AppDataPathMigrationTests : IDisposable
    {
        private readonly AppDataConfig _appDataConfig;

        public AppDataPathMigrationTests()
        {
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            _appDataConfig = new AppDataConfig { BasePath = tmp };
        }

        public void Dispose()
        {
            Directory.Delete(_appDataConfig.BasePath, true);
        }

        private IServiceProvider ConfigureServiceProvider(IEnumerable<IAppDataMigration> migrations)
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AppDataPath:BasePath"] = _appDataConfig.BasePath
            });

            builder.AddAppData();

            foreach (var mig in migrations)
            {
                builder.Services.AddSingleton(mig);
            }

            return builder.Build().Services;
        }

        [Fact]
        public async Task TestNoMigration()
        {
            var serviceProvider = ConfigureServiceProvider(Array.Empty<IAppDataMigration>());

            await serviceProvider.GetService<IAppDataMigrator>().Migrate();

            Assert.Equal(new Version(), serviceProvider.GetService<AppData>().CurrentVersion);
        }

        [Fact]
        public async Task TestCreateSingleFileMigration()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(
                    new Version(1, 0),
                    "test.txt",
                    "test")
            });

            await serviceProvider.GetService<IAppDataMigrator>().Migrate();

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(1, 0), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("test", File.ReadAllText(appData.GetDataFileName("test.txt")));
        }

        [Fact]
        public async Task TestCreateTwoFilesMigration()
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

            await serviceProvider.GetService<IAppDataMigrator>().Migrate();

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(1, 1), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("test", File.ReadAllText(appData.GetDataFileName("test.txt")));
            Assert.True(File.Exists(appData.GetDataFileName("test2.txt")));
            Assert.Equal("test2", File.ReadAllText(appData.GetDataFileName("test2.txt")));
        }

        [Fact]
        public async Task TestCreateTwoFilesMigrationUnordered()
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

            await serviceProvider.GetService<IAppDataMigrator>().Migrate();

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(2, 0), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("replaced", File.ReadAllText(appData.GetDataFileName("test.txt")));
        }

        [Fact]
        public async Task TestCreateTwoFilesMigrationInTwoSteps()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(
                    new Version(1, 0),
                    "test.txt",
                    "test")
            });

            await serviceProvider.GetService<IAppDataMigrator>().Migrate();

            serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(
                    new Version(1, 1),
                    "test2.txt",
                    "test2")
            });

            await serviceProvider.GetService<IAppDataMigrator>().Migrate();

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(1, 1), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("test", File.ReadAllText(appData.GetDataFileName("test.txt")));
            Assert.True(File.Exists(appData.GetDataFileName("test2.txt")));
            Assert.Equal("test2", File.ReadAllText(appData.GetDataFileName("test2.txt")));
        }

        [Fact]
        public async Task TestErrorInFirstMigration()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new ThrowExceptionMigration<IOException>(new Version(1, 0))
            });

            await Assert.ThrowsAsync<IOException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate().AsTask());

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(), appData.CurrentVersion);
        }

        [Fact]
        public async Task TestErrorRollback()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(
                    new Version(1, 0),
                    "test.txt",
                    "test"),
                new ThrowExceptionMigration<IOException>(new Version(1, 1))
            });

            await Assert.ThrowsAsync<IOException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate().AsTask());

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(), appData.CurrentVersion);
            Assert.False(File.Exists(appData.GetDataFileName("test.txt")));
        }

        [Fact]
        public async Task TestErrorRollbackWithPreviousMigration()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(
                    new Version(1, 0),
                    "test.txt",
                    "original")
            });

            await serviceProvider.GetService<IAppDataMigrator>().Migrate();

            serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(
                    new Version(2, 1),
                    "test.txt",
                    "changed !"),
                new ThrowExceptionMigration<IOException>(new Version(2, 2))
            });

            await Assert.ThrowsAsync<IOException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate().AsTask());

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(1, 0), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("original", File.ReadAllText(appData.GetDataFileName("test.txt")));
        }

        [Fact]
        public async Task TestDuplicateMigrationForSameVersionThrowsException()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(new Version(1, 0), "test.txt", "original"),
                new OverwriteFileMigration(new Version(1, 0), "test2.txt", "coucou")
            });

            await Assert.ThrowsAsync<Exception>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate().AsTask());
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

            public async ValueTask Upgrade(AppData appData, IServiceProvider serviceProvider)
            {
                await File.WriteAllTextAsync(appData.GetDataFileName(_fileName), _content);
            }
        }

        public class ThrowExceptionMigration<T> : IAppDataMigration where T : Exception, new()
        {
            public ThrowExceptionMigration(Version version)
            {
                Version = version;
            }

            public Version Version { get; }

            public ValueTask Upgrade(AppData appData, IServiceProvider serviceProvider)
            {
                throw new T();
            }
        }
    }
}