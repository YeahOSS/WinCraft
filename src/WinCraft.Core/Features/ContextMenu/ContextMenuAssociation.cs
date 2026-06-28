using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Describes one registry association path that can own menu items.
    /// </summary>
    internal sealed class ContextMenuAssociation(ContextMenuAssociationKind kind, RegistryPath registryPath)
    {
        public ContextMenuAssociationKind Kind { get; } = kind;

        public RegistryPath RegistryPath { get; } = registryPath;
    }
}
