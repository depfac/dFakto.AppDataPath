using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace dFakto.AppDataPath.Tests
{
    public class AppDataPathMigrationTests : IDisposable
    {
        private readonly AppDataConfig _appDataConfig = new AppDataConfig();

        public AppDataPathMigrationTests()
        {
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            _appDataConfig.BasePath = tmp;
        }

        public void Dispose()
        {
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

            Assert.Equal(new Version(), serviceProvider.GetService<AppData>().CurrentVersion);
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

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(1, 0), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("test", File.ReadAllText(appData.GetDataFileName("test.txt")));
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

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(1, 1), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("test", File.ReadAllText(appData.GetDataFileName("test.txt")));
            Assert.True(File.Exists(appData.GetDataFileName("test2.txt")));
            Assert.Equal("test2", File.ReadAllText(appData.GetDataFileName("test2.txt")));
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

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(2, 0), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("replaced", File.ReadAllText(appData.GetDataFileName("test.txt")));
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

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(1, 1), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("test", File.ReadAllText(appData.GetDataFileName("test.txt")));
            Assert.True(File.Exists(appData.GetDataFileName("test2.txt")));
            Assert.Equal("test2", File.ReadAllText(appData.GetDataFileName("test2.txt")));
        }

        [Fact]
        public void TestErrorInFirstMigration()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new ThrowExceptionMigration<IOException>(new Version(1, 0))
            });

            Assert.Throws<IOException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate());

            var appData = serviceProvider.GetService<AppData>();

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

            Assert.Throws<IOException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate());

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(), appData.CurrentVersion);
            Assert.False(File.Exists(appData.GetDataFileName("test.txt")));
        }

        [Fact]
        public void TestErrorRollbackWithPreviousMigration()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(
                    new Version(1, 0),
                    "test.txt",
                    "original")
            });

            serviceProvider.GetService<IAppDataMigrator>().Migrate();

            serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(
                    new Version(2, 1),
                    "test.txt",
                    "changed !"),
                new ThrowExceptionMigration<IOException>(new Version(2, 2))
            });

            Assert.Throws<IOException>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate());

            var appData = serviceProvider.GetService<AppData>();

            Assert.Equal(new Version(1, 0), appData.CurrentVersion);
            Assert.True(File.Exists(appData.GetDataFileName("test.txt")));
            Assert.Equal("original", File.ReadAllText(appData.GetDataFileName("test.txt")));
        }

        [Fact]
        public void TestDuplicateMigrationForSameVersionThrowsException()
        {
            var serviceProvider = ConfigureServiceProvider(new IAppDataMigration[]
            {
                new OverwriteFileMigration(new Version(1, 0), "test.txt", "original"),
                new OverwriteFileMigration(new Version(1, 0), "test2.txt", "coucou")
            });

            Assert.Throws<Exception>(() => serviceProvider.GetService<IAppDataMigrator>().Migrate());
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

            public void Upgrade(AppData appData, IServiceProvider serviceProvider)
            {
                File.WriteAllText(appData.GetDataFileName(_fileName), _content);
            }
        }

        public class ThrowExceptionMigration<T> : IAppDataMigration where T : Exception, new()
        {
            public ThrowExceptionMigration(Version version)
            {
                Version = version;
            }

            public Version Version { get; }

            public void Upgrade(AppData appData, IServiceProvider serviceProvider)
            {
                throw new T();
            }
        }
    }
}