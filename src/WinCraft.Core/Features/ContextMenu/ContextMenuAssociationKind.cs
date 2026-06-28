namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Identifies the registry association that owns a context menu item.
    /// </summary>
    internal enum ContextMenuAssociationKind
    {
        File,
        Directory,
        Folder,
        AllFilesystemObjects,
        DirectoryBackground,
        DesktopBackground,
        Drive,
        Computer,
        RecycleBin,
        LibraryFolder,
        LibraryFolderBackground,
        UserLibraryFolder,
        SystemAssoc,
        ProgramAssoc,
        Uwp,
        Unknown,
        Image,
        Audio,
        Video,
        Text,
        Document,
        Compressed,
        System,
        ImageDirectory,
        AudioDirectory,
        VideoDirectory,
        DocumentDirectory,
        SystemCommand,
        UserCommand,
        ShellNew,
        SendTo,
        OpenWith,
        WinX
    }
}
