namespace dFakto.AppDataPath
{
    public class AppDataConfig
    {
        public string? BasePath { get; set; }
        public bool CleanupTempFileOnClose { get; set; } = true;
    }
}