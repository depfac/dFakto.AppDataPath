namespace dFakto.AppDataPath
{
    public class AppDataPathConfig
    {
        public string BasePath { get; set; }
        public bool CleanupTempFileOnClose { get; set; } = true;
    }
}