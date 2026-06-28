namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Read-only snapshot of a shell context menu entry.
    /// </summary>
    internal sealed class ContextMenuItem
    {
        public ContextMenuItemKind Kind { get; set; }

        public ContextMenuAssociationKind AssociationKind { get; set; }

        public string RegistryPath { get; set; }

        public string AssociationRegistryPath { get; set; }

        public string ContainerRegistryPath { get; set; }

        public string ContainerRelativePath { get; set; }

        public string KeyName { get; set; }

        public string Text { get; set; }

        public string Icon { get; set; }

        public string Command { get; set; }

        public ContextMenuCommandPosition Position { get; set; }

        public bool ShowIcon { get; set; }

        public bool HasLuaShield { get; set; }

        public bool OnlyWithShift { get; set; }

        public bool OnlyInExplorer { get; set; }

        public bool NoWorkingDirectory { get; set; }

        public bool NeverDefault { get; set; }

        public string FilePath { get; set; }

        public string TargetPath { get; set; }

        public string ClassName { get; set; }

        public string GroupName { get; set; }

        public bool Visible { get; set; }

        public bool IsDragDrop { get; set; }

        public bool IsSubItem { get; set; }

        public bool HasSubItems { get; set; }
    }
}
