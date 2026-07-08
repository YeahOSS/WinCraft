namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Resolved metadata for a COM class identified by its CLSID (GUID).
    /// </summary>
    internal struct ClsidInfo
    {
        public string Clsid;

        public string Text;

        public IconLocation Icon;

        public string FilePath;
    }
}
