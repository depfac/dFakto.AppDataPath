using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace dFakto.AppDataPath.Tests;

public class AppDataPathTest
{
    private static object _lock = new();

    private readonly string _customConfigDir;
    private readonly IHost _customValuesHost;
    private readonly IHost _defaultValuesHost;

    public AppDataPathTest()
    {
        // Create custom directory with configs
        var appDataDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _customConfigDir = Path.Combine(appDataDir, "config");
        Directory.CreateDirectory(appDataDir);
        Directory.CreateDirectory(_customConfigDir);

        using (var sw = new StreamWriter(File.Open(Path.Combine(_customConfigDir, "test.json"), FileMode.Append,
                   FileAccess.Write)))
        {
            sw.Write("{\"a\":{\"b\":\"doughnut\"},\"c\":\"donut\"}");
        }

        // Global lock to avoid clashes on environment variable parsing.
        // (Since environment variables are global to the process)
        lock (_lock)
        {
            // Unset env variables, for default values
            Environment.SetEnvironmentVariable("DOTNET_AppDataPath:BasePath", null);
            _defaultValuesHost = CreateHostBuilder().Build();

            // Set env variable, for custom values
            Environment.SetEnvironmentVariable("DOTNET_AppDataPath:BasePath", appDataDir);
            _customValuesHost = CreateHostBuilder().Build();

            Environment.SetEnvironmentVariable("DOTNET_AppDataPath:BasePath", null);
        }
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .AddAppData("AppDataPath");
    }

    [Fact]
    public void AppDataDefaultDirectoryTest()
    {
        var config = _defaultValuesHost.Services.GetService<IAppData>();
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.None);
        basePath = Path.Combine(basePath, Assembly.GetEntryAssembly()?.GetName().Name ?? "", "config");
        Assert.Equal(basePath, config.ConfigPath);
    }

    [Fact]
    public void AppDataCustomDirectoryTest()
    {
        var config = _customValuesHost.Services.GetService<IAppData>();
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
        var appdata = _customValuesHost.Services.GetService<IAppData>();
        var fileName = Path.GetRandomFileName();
        appdata.WriteAllText("cheese", AppDataDir.Temp, fileName);
        Assert.Equal("cheese", appdata.ReadAllText(AppDataDir.Temp, fileName));
    }

    [Fact]
    public void AppDataEscapeBasePath()
    {
        var appdata = _customValuesHost.Services.GetService<IAppData>();

        // This should succeed, because we go back into the valid path
        var basePathName = Path.GetFileName(appdata.BasePath);
        appdata.GetFilePath("..", basePathName);

        // This should fail, because we try to escape
        Assert.Throws<ArgumentException>(() => appdata.GetFilePath("..", "cheese"));
    }
}