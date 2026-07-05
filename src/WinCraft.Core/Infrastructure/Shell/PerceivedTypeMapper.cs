using WinCraft.Infrastructure.RegistryAccess;
using Windows.Win32.UI.Shell.Common;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Maps <see cref="PERCEIVED"/> values to their
    /// <c>SystemFileAssociations</c> registry subkey names.
    /// </summary>
    internal static class PerceivedTypeMapper
    {
        private const string SystemFileAssociationsPrefix = @"SystemFileAssociations\";

        internal static readonly PerceivedTypeEntry[] Entries =
        {
            new(PERCEIVED.PERCEIVED_TYPE_TEXT, "Text"),
            new(PERCEIVED.PERCEIVED_TYPE_IMAGE, "Image"),
            new(PERCEIVED.PERCEIVED_TYPE_AUDIO, "Audio"),
            new(PERCEIVED.PERCEIVED_TYPE_VIDEO, "Video"),
            new(PERCEIVED.PERCEIVED_TYPE_COMPRESSED, "Compressed"),
            new(PERCEIVED.PERCEIVED_TYPE_DOCUMENT, "Document"),
            new(PERCEIVED.PERCEIVED_TYPE_SYSTEM, "System"),
            new(PERCEIVED.PERCEIVED_TYPE_APPLICATION, "Application"),
            new(PERCEIVED.PERCEIVED_TYPE_GAMEMEDIA, "GameMedia"),
            new(PERCEIVED.PERCEIVED_TYPE_CONTACTS, "Contacts"),
        };

        /// <summary>
        /// Returns a <see cref="RegistryPath"/> under
        /// <c>HKEY_CLASSES_ROOT\SystemFileAssociations\<paramref name="name"/></c>.
        /// </summary>
        internal static RegistryPath FromClassesRootSystemFileAssociations(string name)
        {
            return RegistryPath.FromClassesRoot(SystemFileAssociationsPrefix + name);
        }

        /// <summary>
        /// Maps a <see cref="PERCEIVED"/> value to its
        /// <c>SystemFileAssociations</c> subkey name.
        /// Returns <c>false</c> for types with no known mapping
        /// (e.g. <see cref="PERCEIVED.PERCEIVED_TYPE_FOLDER"/>).
        /// </summary>
        internal static bool TryMap(PERCEIVED perceived, out string name)
        {
            foreach (var entry in Entries)
            {
                if (entry.Perceived == perceived)
                {
                    name = entry.Name;
                    return true;
                }
            }

            name = null;
            return false;
        }

        internal sealed class PerceivedTypeEntry(PERCEIVED perceived, string name)
        {
            public readonly PERCEIVED Perceived = perceived;
            public readonly string Name = name;
        }
    }
}
