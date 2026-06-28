using System;
using System.Collections.Generic;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Maps context menu surfaces to the registry associations used by Explorer.
    /// </summary>
    internal static class ContextMenuAssociationPlanner
    {
        public static List<ContextMenuAssociation> GetAssociations(ContextMenuScope scope)
        {
            var associations = new List<ContextMenuAssociation>();
            AddAssociations(scope, associations);
            return associations;
        }

        private static void AddAssociations(ContextMenuScope scope, List<ContextMenuAssociation> associations)
        {
            switch (scope)
            {
                case ContextMenuScope.File:
                    Add(associations, ContextMenuAssociationKind.File, "*");
                    break;
                case ContextMenuScope.Directory:
                    Add(associations, ContextMenuAssociationKind.Directory, "Directory");
                    Add(associations, ContextMenuAssociationKind.Folder, "Folder");
                    break;
                case ContextMenuScope.Background:
                    Add(associations, ContextMenuAssociationKind.DirectoryBackground, @"Directory\Background");
                    if (WindowsVersion.IsAtLeast(WindowsRelease.Win7))
                        Add(associations, ContextMenuAssociationKind.DesktopBackground, "DesktopBackground");
                    break;
                case ContextMenuScope.Drive:
                    Add(associations, ContextMenuAssociationKind.Drive, "Drive");
                    break;
                case ContextMenuScope.AllFilesystemObjects:
                    Add(associations, ContextMenuAssociationKind.AllFilesystemObjects, "AllFilesystemObjects");
                    break;
                case ContextMenuScope.Computer:
                    Add(associations, ContextMenuAssociationKind.Computer, @"CLSID\{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
                    break;
                case ContextMenuScope.RecycleBin:
                    Add(associations, ContextMenuAssociationKind.RecycleBin, @"CLSID\{645FF040-5081-101B-9F08-00AA002F954E}");
                    break;
                case ContextMenuScope.Library:
                    if (WindowsVersion.IsAtLeast(WindowsRelease.Win7))
                    {
                        Add(associations, ContextMenuAssociationKind.LibraryFolder, "LibraryFolder");
                        Add(associations, ContextMenuAssociationKind.LibraryFolderBackground, @"LibraryFolder\Background");
                        Add(associations, ContextMenuAssociationKind.UserLibraryFolder, "UserLibraryFolder");
                    }
                    break;
                case ContextMenuScope.Shortcut:
                    Add(associations, ContextMenuAssociationKind.SystemAssoc, @"SystemFileAssociations\.lnk");
                    Add(associations, ContextMenuAssociationKind.Uwp, "Launcher.ImmersiveApplication");
                    break;
                case ContextMenuScope.PerceivedType:
                    Add(associations, ContextMenuAssociationKind.Unknown, "Unknown");
                    Add(associations, ContextMenuAssociationKind.Image, @"SystemFileAssociations\Image");
                    Add(associations, ContextMenuAssociationKind.Audio, @"SystemFileAssociations\Audio");
                    Add(associations, ContextMenuAssociationKind.Video, @"SystemFileAssociations\Video");
                    Add(associations, ContextMenuAssociationKind.Text, @"SystemFileAssociations\Text");
                    Add(associations, ContextMenuAssociationKind.Document, @"SystemFileAssociations\Document");
                    Add(associations, ContextMenuAssociationKind.Compressed, @"SystemFileAssociations\Compressed");
                    Add(associations, ContextMenuAssociationKind.System, @"SystemFileAssociations\System");
                    break;
                case ContextMenuScope.DirectoryType:
                    Add(associations, ContextMenuAssociationKind.ImageDirectory, @"SystemFileAssociations\Directory.Image");
                    Add(associations, ContextMenuAssociationKind.AudioDirectory, @"SystemFileAssociations\Directory.Audio");
                    Add(associations, ContextMenuAssociationKind.VideoDirectory, @"SystemFileAssociations\Directory.Video");
                    Add(associations, ContextMenuAssociationKind.DocumentDirectory, @"SystemFileAssociations\Directory.Document");
                    break;
                case ContextMenuScope.DragDrop:
                    Add(associations, ContextMenuAssociationKind.Directory, "Directory");
                    Add(associations, ContextMenuAssociationKind.Folder, "Folder");
                    Add(associations, ContextMenuAssociationKind.Drive, "Drive");
                    Add(associations, ContextMenuAssociationKind.AllFilesystemObjects, "AllFilesystemObjects");
                    break;
                case ContextMenuScope.CommandStore or ContextMenuScope.ShellNew or ContextMenuScope.SendTo
                    or ContextMenuScope.OpenWith or ContextMenuScope.WinX or ContextMenuScope.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope));
            }
        }

        private static void Add(
            List<ContextMenuAssociation> associations,
            ContextMenuAssociationKind kind,
            string classesRootSubKeyPath)
        {
            associations.Add(new ContextMenuAssociation(kind, RegistryPath.ClassesRoot(classesRootSubKeyPath)));
        }
    }
}
