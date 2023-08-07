using System.Threading.Tasks;

namespace dFakto.AppDataPath
{
    /// <summary>
    /// Class responsible to execute the migrations over the AppData folder
    /// Backup and restore of the appData folder is performed automatically to ensure integrity
    /// </summary>
    public interface IAppDataMigrator
    {
        /// <summary>
        /// Execute the migration if required
        /// </summary>
        ValueTask Migrate();
    }
}