using System;

namespace dFakto.AppDataPath
{
    /// <summary>
    /// Interface representing a Migration that must be applied on the data folder
    /// </summary>
    public interface IAppDataMigration
    {
        /// <summary>
        /// The Migration Version number
        /// </summary>
        Version Version { get; }
        
        /// <summary>
        /// The method that will Update the data folder to the specified version.
        /// <remarks>In an exception is thrown, the app data folder will be rollback and the migration will be tried again at next startup</remarks>
        /// </summary>
        /// <param name="appData">The AppData that needs to be migrated</param>
        /// <param name="serviceProvider">Dependency injection</param>
        void Upgrade(AppData appData, IServiceProvider serviceProvider);
    }
}