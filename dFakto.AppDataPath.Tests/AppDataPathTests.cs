using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace dFakto.AppDataPath.Tests
{
    public class AppDataPathTest
    {
        private ServiceProvider _serviceProvider;
        private string _customConfigDir;
        private IHost _defaultValuesHost;
        private IHost _customValuesHost;

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .AddAppData("AppDataPath");

        public AppDataPathTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            _serviceProvider = services.BuildServiceProvider();

            // Create custom directory with configs
            string appDataDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _customConfigDir = Path.Combine(appDataDir, "config");
            Directory.CreateDirectory(appDataDir);
            Directory.CreateDirectory(_customConfigDir);

            using (var sw = new StreamWriter(File.Open(Path.Combine(_customConfigDir, "test.json"), FileMode.Append,
                FileAccess.Write)))
            {
                sw.Write("{\"a\":{\"b\":\"doughnut\"},\"c\":\"donut\"}");
            }

            // Unset env variables, for default values
            System.Environment.SetEnvironmentVariable("DOTNET_AppDataPath:BasePath", null);
            _defaultValuesHost = CreateHostBuilder().Build();

            // Set env variable, for custom values
            System.Environment.SetEnvironmentVariable("DOTNET_AppDataPath:BasePath", appDataDir);
            _customValuesHost = CreateHostBuilder().Build();
        }

        [Fact]
        public void AppDataDefaultDirectoryTest()
        {
            var config = _defaultValuesHost.Services.GetService<AppData>();
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.None);
            basePath = Path.Combine(basePath, System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "", "config");
            Assert.Equal(basePath, config.ConfigPath);
        }

        [Fact]
        public void AppDataCustomDirectoryTest()
        {
            var config = _customValuesHost.Services.GetService<AppData>();
            Assert.Equal(_customConfigDir, config.ConfigPath);
        }

        [Fact]
        public void AppDataConfigLoadTest()
        {
            var config = _customValuesHost.Services.GetService<IConfiguration>();
            Assert.Equal("doughnut", config.GetSection("a").GetSection("b").Value);
        }

        [Fact]
        public void AppDataCreateTempFile()
        {
            var appdata = _customValuesHost.Services.GetService<AppData>();
            using (var fs = appdata.OpenTempFile())
            {
                var sw = new StreamWriter(fs);
                var sr = new StreamReader(fs);
                sw.Write("cheese");
                sw.Flush();
                fs.Seek(0, SeekOrigin.Begin);
                Assert.Equal("cheese", sr.ReadToEnd());
            }
        }

        [Fact]
        public void AppDataDeleteTemp()
        {
            var appdata = _customValuesHost.Services.GetService<AppData>();
            var fileName = appdata.GetTempFileName();
            File.Create(fileName);
            Assert.True(File.Exists(fileName));
            appdata.EmptyTemp();
            Assert.False(File.Exists(fileName));
        }
    }
}
