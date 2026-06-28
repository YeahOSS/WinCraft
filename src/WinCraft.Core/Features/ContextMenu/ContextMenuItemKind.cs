namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Identifies the backing mechanism for a context menu item.
    /// </summary>
    internal enum ContextMenuItemKind
    {
        ShellCommand,
        ShellExtension,
        ShellNew,
        SendTo,
        OpenWith,
        WinX
    }
}
