namespace dFakto.AppDataPath
{
    /// <summary>
    /// Main Configuration Flags
    /// </summary>
    public class AppDataConfig
    {
        /// <summary>
        /// Path to the directory used to store all application files.
        /// </summary>
        public string? BasePath { get; init; }

        /// <summary>
        /// Delete all temp files when disposing. Default to true
        /// </summary>
        public bool CleanupTempFileOnClose { get; init; } = true;
    }
}