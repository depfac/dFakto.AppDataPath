namespace dFakto.AppDataPath;

public interface IAppDataMigrator
{
    /// <summary>
    /// Run the necessary migrations to get the appdata path to the latest version.
    /// </summary>
    void Migrate();
}