namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Identifies the shell context menu surface to enumerate.
    /// </summary>
    internal enum ContextMenuScope
    {
        None,
        File,
        Directory,
        Background,
        Drive,
        AllFilesystemObjects,
        Computer,
        RecycleBin,
        Library,
        Shortcut,
        PerceivedType,
        DirectoryType,
        CommandStore,
        DragDrop,
        ShellNew,
        SendTo,
        OpenWith,
        WinX
    }
}
