using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using WinCraft.Features.ContextMenu;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Tests.Features.ContextMenu
{
    [TestFixture]
    internal sealed class ContextMenuCatalogTests
    {
        [Test]
        public void Load_FileScope_ReadsShellAndShellExItems()
        {
            var registry = new FakeRegistryReader();
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell");
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell\open");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\open", "MUIVerb", "Open with Test");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\open", "Icon", "test.exe,1");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\open", "Position", "Bottom");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\open", "Extended", string.Empty);
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\open", "OnlyInBrowserWindow", string.Empty);
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\open", "NoWorkingDirectory", string.Empty);
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\open", "NeverDefault", string.Empty);
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\open\Command", null, @"""C:\Tools\Test.exe"" ""%1""");
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\ShellEx\ContextMenuHandlers");
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\ShellEx\ContextMenuHandlers\{00000000-0000-0000-0000-000000000001}");

            var catalog = new ContextMenuCatalog(registry, null, null);

            var items = catalog.Load(ContextMenuScope.File);

            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items[0].Kind, Is.EqualTo(ContextMenuItemKind.ShellCommand));
            Assert.That(items[0].Visible, Is.True);
            Assert.That(items[0].Text, Is.EqualTo("Open with Test"));
            Assert.That(items[0].FilePath, Is.EqualTo(@"C:\Tools\Test.exe"));
            Assert.That(items[0].Icon, Is.EqualTo("test.exe,1"));
            Assert.That(items[0].Position, Is.EqualTo(ContextMenuCommandPosition.Bottom));
            Assert.That(items[0].ShowIcon, Is.True);
            Assert.That(items[0].OnlyWithShift, Is.True);
            Assert.That(items[0].OnlyInExplorer, Is.True);
            Assert.That(items[0].NoWorkingDirectory, Is.True);
            Assert.That(items[0].NeverDefault, Is.True);
            Assert.That(items[1].Kind, Is.EqualTo(ContextMenuItemKind.ShellExtension));
            Assert.That(items[1].Visible, Is.True);
        }

        [Test]
        public void Load_HiddenShellCommand_ReturnsVisibleFalse()
        {
            var registry = new FakeRegistryReader();
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell");
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell\hidden");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\hidden", "ProgrammaticAccessOnly", string.Empty);

            var catalog = new ContextMenuCatalog(registry, null, null);

            var items = catalog.Load(ContextMenuScope.File);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].Visible, Is.False);
        }

        [Test]
        public void Load_CommandFlagsHiddenShellCommand_ReturnsVisibleFalse()
        {
            var registry = new FakeRegistryReader();
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell");
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell\hidden");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\hidden", "CommandFlags", 8);

            var catalog = new ContextMenuCatalog(registry, null, null);

            var items = catalog.Load(ContextMenuScope.File);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].Visible, Is.False);
        }

        [Test]
        public void LoadSubItems_InlineShellSubCommands_ReadsChildItems()
        {
            var registry = new FakeRegistryReader();
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell");
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell\parent");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\parent", "SubCommands", string.Empty);
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell\parent\shell\child");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\parent\shell\child", "MUIVerb", "Child command");

            var catalog = new ContextMenuCatalog(registry, null, null);
            var parent = catalog.Load(ContextMenuScope.File)[0];

            var items = catalog.LoadSubItems(parent);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].IsSubItem, Is.True);
            Assert.That(items[0].Text, Is.EqualTo("Child command"));
        }

        [Test]
        public void LoadForExtension_ReadsSystemProgramAndPerceivedAssociations()
        {
            var registry = new FakeRegistryReader();
            registry.SetValue(@"HKEY_CLASSES_ROOT\.abc", null, "abcfile");
            registry.SetValue(@"HKEY_CLASSES_ROOT\.abc", "PerceivedType", "Image");
            registry.AddKey(@"HKEY_CLASSES_ROOT\SystemFileAssociations\.abc\Shell\sys");
            registry.AddKey(@"HKEY_CLASSES_ROOT\abcfile\Shell\prog");
            registry.AddKey(@"HKEY_CLASSES_ROOT\SystemFileAssociations\Image\Shell\image");

            var catalog = new ContextMenuCatalog(registry, null, null);

            var items = catalog.LoadForExtension(".abc");

            Assert.That(items.Count, Is.EqualTo(3));
            Assert.That(items[0].AssociationKind, Is.EqualTo(ContextMenuAssociationKind.SystemAssoc));
            Assert.That(items[1].AssociationKind, Is.EqualTo(ContextMenuAssociationKind.ProgramAssoc));
            Assert.That(items[2].AssociationKind, Is.EqualTo(ContextMenuAssociationKind.Image));
        }

        [Test]
        public void Load_ShellCommandWithDelegateExecute_ReadsClsidMetadata()
        {
            const string clsid = "{00000000-0000-0000-0000-000000000010}";
            var registry = new FakeRegistryReader();
            registry.AddKey(@"HKEY_CLASSES_ROOT\*\Shell\handler");
            registry.SetValue(@"HKEY_CLASSES_ROOT\*\Shell\handler\Command", "DelegateExecute", clsid);
            registry.SetValue(@"HKEY_CLASSES_ROOT\CLSID\" + clsid, "LocalizedString", "Handler Text");
            registry.SetValue(@"HKEY_CLASSES_ROOT\CLSID\" + clsid + @"\DefaultIcon", null, "handler.dll,3");
            registry.SetValue(@"HKEY_CLASSES_ROOT\CLSID\" + clsid + @"\InprocServer32", null, @"C:\Tools\handler.dll");

            var catalog = new ContextMenuCatalog(registry, null, null);

            var items = catalog.Load(ContextMenuScope.File);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].Text, Is.EqualTo("Handler Text"));
            Assert.That(items[0].Icon, Is.EqualTo("handler.dll,3"));
            Assert.That(items[0].FilePath, Is.EqualTo(@"C:\Tools\handler.dll"));
        }

        [Test]
        public void Load_OpenWith_SkipsCommandsForDifferentExecutable()
        {
            var registry = new FakeRegistryReader();
            registry.SetValue(
                @"HKEY_CLASSES_ROOT\Applications\sample.exe\shell\open\command",
                null,
                @"""C:\Tools\sample.exe"" ""%1""");
            registry.SetValue(
                @"HKEY_CLASSES_ROOT\Applications\other.exe\shell\open\command",
                null,
                @"""C:\Tools\sample.exe"" ""%1""");

            var catalog = new ContextMenuCatalog(registry, null, null);

            var items = catalog.Load(ContextMenuScope.OpenWith);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].KeyName, Is.EqualTo("sample.exe"));
        }

        [Test]
        public void Load_SendTo_UsesDesktopIniLocalizedName()
        {
            string directoryPath = Path.Combine(Path.GetTempPath(), "WinCraft_SendTo_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directoryPath);
            try
            {
                string itemPath = Path.Combine(directoryPath, "sample.txt");
                File.WriteAllText(itemPath, "content");
                File.WriteAllText(
                    Path.Combine(directoryPath, "desktop.ini"),
                    "[LocalizedFileNames]" + Environment.NewLine + "sample.txt=Localized Sample" + Environment.NewLine);
                var catalog = new ContextMenuCatalog(new FakeRegistryReader(), directoryPath, null);

                var items = catalog.Load(ContextMenuScope.SendTo);

                Assert.That(items.Count, Is.EqualTo(1));
                Assert.That(items[0].Text, Is.EqualTo("Localized Sample"));
            }
            finally
            {
                if (Directory.Exists(directoryPath))
                    Directory.Delete(directoryPath, recursive: true);
            }
        }

        [Test]
        public void Load_ShellNew_UsesClassesMultiStringForVisibility()
        {
            var registry = new FakeRegistryReader();
            registry.AddKey(@"HKEY_CLASSES_ROOT");
            registry.AddKey(@"HKEY_CLASSES_ROOT\.abc");
            registry.SetValue(@"HKEY_CLASSES_ROOT\.abc", null, "abcfile");
            registry.AddKey(@"HKEY_CLASSES_ROOT\.abc\ShellNew");
            registry.SetValue(@"HKEY_CLASSES_ROOT\.abc\ShellNew", "NullFile", string.Empty);
            registry.AddKey(@"HKEY_CLASSES_ROOT\abcfile");
            registry.SetValue(@"HKEY_CLASSES_ROOT\abcfile", null, "ABC File");
            registry.SetValue(
                @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew",
                "Classes",
                new[] { ".abc" });

            var catalog = new ContextMenuCatalog(registry, null, null);

            var items = catalog.Load(ContextMenuScope.ShellNew);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].Text, Is.EqualTo("ABC File"));
            Assert.That(items[0].Visible, Is.True);
        }

        private sealed class FakeRegistryReader : IRegistryReader
        {
            private readonly Dictionary<string, Dictionary<string, object>> _keys =
                new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

            public void AddKey(string path)
            {
                if (!_keys.ContainsKey(path))
                    _keys.Add(path, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));

                int separatorIndex = path.LastIndexOf('\\');
                if (separatorIndex > 0)
                    AddKey(path.Substring(0, separatorIndex));
            }

            public void SetValue(string path, string valueName, object value)
            {
                AddKey(path);
                _keys[path][valueName ?? string.Empty] = value;
            }

            public bool KeyExists(RegistryPath path)
            {
                return _keys.ContainsKey(path.ToString().TrimEnd('\\'));
            }

            public string[] GetSubKeyNames(RegistryPath path)
            {
                string prefix = path.ToString().TrimEnd('\\');
                string childPrefix = prefix.Length == 0 ? string.Empty : prefix + "\\";
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string keyPath in _keys.Keys)
                {
                    if (!keyPath.StartsWith(childPrefix, StringComparison.OrdinalIgnoreCase))
                        continue;
                    string remainder = keyPath.Substring(childPrefix.Length);
                    if (remainder.Length == 0 || remainder.IndexOf('\\') >= 0)
                        continue;
                    names.Add(remainder);
                }

                var result = new string[names.Count];
                names.CopyTo(result);
                return result;
            }

            public string[] GetValueNames(RegistryPath path)
            {
                if (!_keys.TryGetValue(path.ToString().TrimEnd('\\'), out Dictionary<string, object> values))
                    return new string[0];

                var names = new List<string>();
                foreach (string name in values.Keys)
                    names.Add(name);
                return names.ToArray();
            }

            public object GetValue(RegistryPath path, string valueName)
            {
                if (!_keys.TryGetValue(path.ToString().TrimEnd('\\'), out Dictionary<string, object> values))
                    return null;
                values.TryGetValue(valueName ?? string.Empty, out object value);
                return value;
            }
        }
    }
}
