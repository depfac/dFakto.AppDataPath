using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace dFakto.AppDataPath
{
    public class DefaultAppDataMigrationProvider : IAppDataMigrationProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultAppDataMigrationProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEnumerable<IAppDataMigration> GetAppDataMigration()
        {
            return _serviceProvider.GetServices<IAppDataMigration>();
        }
    }
}