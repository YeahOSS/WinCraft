using System;
using System.IO;
using Microsoft.Win32;
using NUnit.Framework;
using WinCraft.Features.ContextMenu;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Tests.Features.ContextMenu
{
    [TestFixture]
    internal sealed class ContextMenuEditorTests
    {
        private string _testRoot;

        [SetUp]
        public void SetUp()
        {
            _testRoot = @"Software\WinCraft\Tests\ContextMenuEditor_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            RegistryWriter.DeleteKey(new RegistryKeyOperationRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SourceSubKeyPath = _testRoot,
                Recursive = true
            });
        }

        [Test]
        public void SetShellCommandOptions_WritesExpectedValues()
        {
            string itemPath = _testRoot + @"\Shell\sample";
            var item = CreateShellCommandItem(itemPath);
            var editor = new ContextMenuEditor(null);

            Assert.That(editor.SetShellCommandText(item, "Sample").Succeeded, Is.True);
            Assert.That(editor.SetShellCommandIcon(item, "sample.exe,2").Succeeded, Is.True);
            Assert.That(editor.SetShellCommandPosition(item, ContextMenuCommandPosition.Top).Succeeded, Is.True);
            Assert.That(editor.SetShellCommandOnlyWithShift(item, true).Succeeded, Is.True);
            Assert.That(editor.SetShellCommandOnlyInExplorer(item, true).Succeeded, Is.True);
            Assert.That(editor.SetShellCommandNoWorkingDirectory(item, true).Succeeded, Is.True);
            Assert.That(editor.SetShellCommandNeverDefault(item, true).Succeeded, Is.True);

            string fullPath = @"HKEY_CURRENT_USER\" + itemPath;
            Assert.That(Registry.GetValue(fullPath, "MUIVerb", null), Is.EqualTo("Sample"));
            Assert.That(Registry.GetValue(fullPath, "Icon", null), Is.EqualTo("sample.exe,2"));
            Assert.That(Registry.GetValue(fullPath, "Position", null), Is.EqualTo("Top"));
            Assert.That(Registry.GetValue(fullPath, "Extended", null), Is.EqualTo(string.Empty));
            Assert.That(Registry.GetValue(fullPath, "OnlyInBrowserWindow", null), Is.EqualTo(string.Empty));
            Assert.That(Registry.GetValue(fullPath, "NoWorkingDirectory", null), Is.EqualTo(string.Empty));
            Assert.That(Registry.GetValue(fullPath, "NeverDefault", null), Is.EqualTo(string.Empty));
        }

        [Test]
        public void SetShellCommandVisibleTrue_RemovesHideMarkers()
        {
            string itemPath = _testRoot + @"\Shell\sample";
            RegistryWriter.WriteValue(new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = itemPath,
                ValueName = "ProgrammaticAccessOnly",
                ValueData = string.Empty,
                ValueKind = RegistryValueKind.String
            });
            RegistryWriter.WriteValue(new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = itemPath,
                ValueName = "HideBasedOnVelocityId",
                ValueData = "6527944",
                ValueKind = RegistryValueKind.DWord
            });
            RegistryWriter.WriteValue(new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = itemPath,
                ValueName = "CommandFlags",
                ValueData = "8",
                ValueKind = RegistryValueKind.DWord
            });
            var editor = new ContextMenuEditor(null);

            var result = editor.SetShellCommandVisible(CreateShellCommandItem(itemPath), true);

            Assert.That(result.Succeeded, Is.True);
            string fullPath = @"HKEY_CURRENT_USER\" + itemPath;
            Assert.That(Registry.GetValue(fullPath, "ProgrammaticAccessOnly", null), Is.Null);
            Assert.That(Registry.GetValue(fullPath, "HideBasedOnVelocityId", null), Is.Null);
            Assert.That(Registry.GetValue(fullPath, "CommandFlags", null), Is.Null);
        }

        [Test]
        public void SetOpenWithText_WritesVerbFriendlyAppName()
        {
            string commandPath = _testRoot + @"\Applications\sample.exe\shell\open\command";
            var item = new ContextMenuItem
            {
                Kind = ContextMenuItemKind.OpenWith,
                RegistryPath = @"HKEY_CURRENT_USER\" + commandPath
            };
            var editor = new ContextMenuEditor(null);

            var result = editor.SetOpenWithText(item, "Sample App");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                Registry.GetValue(@"HKEY_CURRENT_USER\" + _testRoot + @"\Applications\sample.exe\shell\open", "FriendlyAppName", null),
                Is.EqualTo("Sample App"));
        }

        [Test]
        public void DeleteRegistryBackedItem_DeletesCurrentUserKeyTree()
        {
            string itemPath = _testRoot + @"\Shell\sample";
            RegistryWriter.WriteValue(new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = itemPath + @"\Command",
                ValueName = null,
                ValueData = "sample.exe",
                ValueKind = RegistryValueKind.String
            });
            var editor = new ContextMenuEditor(null);

            var result = editor.DeleteRegistryBackedItem(CreateShellCommandItem(itemPath));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(Registry.GetValue(@"HKEY_CURRENT_USER\" + itemPath + @"\Command", null, null), Is.Null);
        }

        [Test]
        public void SetSendToText_WritesDesktopIniLocalizedName()
        {
            string directoryPath = Path.Combine(Path.GetTempPath(), "WinCraft_SendToEditor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directoryPath);
            try
            {
                string itemPath = Path.Combine(directoryPath, "sample.txt");
                File.WriteAllText(itemPath, "content");
                var editor = new ContextMenuEditor(null);

                var result = editor.SetSendToText(new ContextMenuItem
                {
                    Kind = ContextMenuItemKind.SendTo,
                    FilePath = itemPath
                }, "Localized Sample", elevated: false);

                Assert.That(result.Succeeded, Is.True);
                string desktopIniPath = Path.Combine(directoryPath, "desktop.ini");
                Assert.That(File.ReadAllText(desktopIniPath), Does.Contain("sample.txt=Localized Sample"));
            }
            finally
            {
                if (Directory.Exists(directoryPath))
                    Directory.Delete(directoryPath, recursive: true);
            }
        }

        private ContextMenuItem CreateShellCommandItem(string itemPath)
        {
            return new ContextMenuItem
            {
                Kind = ContextMenuItemKind.ShellCommand,
                AssociationRegistryPath = @"HKEY_CURRENT_USER\" + _testRoot,
                ContainerRelativePath = "Shell",
                KeyName = "sample",
                RegistryPath = @"HKEY_CURRENT_USER\" + itemPath
            };
        }
    }
}
