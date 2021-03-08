using System;

namespace dFakto.AppDataPath
{
    public interface IAppDataMigration
    {
        Version Version { get; }
        void Upgrade(AppData appData, IServiceProvider serviceProvider);
    }
}