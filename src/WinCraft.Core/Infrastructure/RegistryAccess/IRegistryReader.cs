namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Reads registry keys and values without requesting write access.
    /// </summary>
    internal interface IRegistryReader
    {
        bool KeyExists(RegistryPath path);

        string[] GetSubKeyNames(RegistryPath path);

        string[] GetValueNames(RegistryPath path);

        object GetValue(RegistryPath path, string valueName);
    }
}
