using System;
using System.Collections.Generic;

namespace dFakto.AppDataPath
{
    public interface IAppDataMigrationProvider
    {
        IEnumerable<IAppDataMigration> GetAppDataMigration();
    }
}