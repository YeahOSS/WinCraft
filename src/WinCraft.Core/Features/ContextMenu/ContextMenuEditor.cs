using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using WinCraft.Compatibility;
using WinCraft.Infrastructure.FileSystem;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Security;
using WinCraft.Infrastructure.Shell.Shortcuts;

namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Applies explicit context menu changes that may require write privileges.
    /// </summary>
    internal sealed class ContextMenuEditor
    {
        private const string PrimaryShellContainer = "Shell";
        private const string PrimaryShellExContainer = @"ShellEx\ContextMenuHandlers";
        private const string PrimaryDragDropContainer = @"ShellEx\DragDropHandlers";
        private readonly IPrivilegeBroker _privilegeBroker;
        private readonly ElevatedFileOperator _fileOperator;
        private readonly IRegistryReader _registryReader;

        public ContextMenuEditor(IPrivilegeBroker privilegeBroker)
            : this(privilegeBroker, new RegistryReader())
        {
        }

        internal ContextMenuEditor(IPrivilegeBroker privilegeBroker, IRegistryReader registryReader)
        {
            _privilegeBroker = privilegeBroker;
            _fileOperator = new ElevatedFileOperator(privilegeBroker);
            _registryReader = registryReader ?? throw new ArgumentNullException(nameof(registryReader));
        }

        public PrivilegeExecutionResult SetShellCommandText(ContextMenuItem item, string text)
        {
            var path = RequireRegistryItem(item, ContextMenuItemKind.ShellCommand);
            var result = DeleteValue(path, null);
            if (!result.Succeeded)
                return result;
            return WriteValue(path, "MUIVerb", text ?? string.Empty, RegistryValueKind.String);
        }

        public PrivilegeExecutionResult SetShellCommand(ContextMenuItem item, string command)
        {
            var path = RequireRegistryItem(item, ContextMenuItemKind.ShellCommand).Append("Command");
            return WriteValue(path, null, command ?? string.Empty, RegistryValueKind.String);
        }

        public PrivilegeExecutionResult SetShellCommandIcon(ContextMenuItem item, string icon)
        {
            var path = RequireRegistryItem(item, ContextMenuItemKind.ShellCommand);
            var result = DeleteValue(path, "HasLUAShield");
            if (!result.Succeeded)
                return result;

            if (string.IsNullOrEmpty(icon))
                return DeleteValue(path, "Icon");

            return WriteValue(path, "Icon", icon, RegistryValueKind.String);
        }

        public PrivilegeExecutionResult SetShellCommandHasLuaShield(ContextMenuItem item, bool hasLuaShield)
        {
            var path = RequireRegistryItem(item, ContextMenuItemKind.ShellCommand);
            var result = DeleteValue(path, "Icon");
            if (!result.Succeeded)
                return result;

            return hasLuaShield
                ? WriteValue(path, "HasLUAShield", string.Empty, RegistryValueKind.String)
                : DeleteValue(path, "HasLUAShield");
        }

        public PrivilegeExecutionResult SetShellCommandPosition(
            ContextMenuItem item,
            ContextMenuCommandPosition position)
        {
            var path = RequireRegistryItem(item, ContextMenuItemKind.ShellCommand);
            return position == ContextMenuCommandPosition.Default
                ? DeleteValue(path, "Position")
                : WriteValue(path, "Position", position.ToString(), RegistryValueKind.String);
        }

        public PrivilegeExecutionResult SetShellCommandOnlyWithShift(ContextMenuItem item, bool enabled)
        {
            return SetShellCommandMarkerValue(item, "Extended", enabled);
        }

        public PrivilegeExecutionResult SetShellCommandOnlyInExplorer(ContextMenuItem item, bool enabled)
        {
            return SetShellCommandMarkerValue(item, "OnlyInBrowserWindow", enabled);
        }

        public PrivilegeExecutionResult SetShellCommandNoWorkingDirectory(ContextMenuItem item, bool enabled)
        {
            return SetShellCommandMarkerValue(item, "NoWorkingDirectory", enabled);
        }

        public PrivilegeExecutionResult SetShellCommandNeverDefault(ContextMenuItem item, bool enabled)
        {
            return SetShellCommandMarkerValue(item, "NeverDefault", enabled);
        }

        public PrivilegeExecutionResult SetShellCommandVisible(ContextMenuItem item, bool visible)
        {
            var path = RequireRegistryItem(item, ContextMenuItemKind.ShellCommand);
            if (visible)
            {
                var moveResult = MoveToPrimaryContainer(item, path, PrimaryShellContainer);
                if (!moveResult.Succeeded)
                    return moveResult;

                var currentPath = GetPathInContainer(item, PrimaryShellContainer) ?? path;
                var result = DeleteValue(currentPath, "LegacyDisable");
                if (!result.Succeeded)
                    return result;
                result = DeleteValue(currentPath, "ProgrammaticAccessOnly");
                if (!result.Succeeded)
                    return result;
                result = DeleteValue(currentPath, "HideBasedOnVelocityId");
                if (!result.Succeeded)
                    return result;
                return DeleteCommandFlagsWhenHidden(currentPath);
            }

            var writeResult = WriteValue(path, "ProgrammaticAccessOnly", string.Empty, RegistryValueKind.String);
            if (!writeResult.Succeeded)
                return writeResult;
            return WriteValue(path, "HideBasedOnVelocityId", "6527944", RegistryValueKind.DWord);
        }

        public PrivilegeExecutionResult SetShellExtensionVisible(ContextMenuItem item, bool visible)
        {
            var path = RequireRegistryItem(item, ContextMenuItemKind.ShellExtension);
            string targetContainer = item.IsDragDrop ? PrimaryDragDropContainer : PrimaryShellExContainer;
            if (!visible)
                targetContainer = item.IsDragDrop ? @"ShellEx\-DragDropHandlers" : @"ShellEx\-ContextMenuHandlers";
            return MoveToPrimaryContainer(item, path, targetContainer);
        }

        public PrivilegeExecutionResult SetOpenWithVisible(ContextMenuItem item, bool visible)
        {
            if (item == null || item.Kind != ContextMenuItemKind.OpenWith)
                throw new ArgumentException("The item must be an Open With entry.", nameof(item));
            if (!RegistryPath.TryParse(item.ContainerRegistryPath, out RegistryPath appPath))
                throw new ArgumentException("The item does not contain a valid application registry path.", nameof(item));

            return visible
                ? DeleteValue(appPath, "NoOpenWith")
                : WriteValue(appPath, "NoOpenWith", string.Empty, RegistryValueKind.String);
        }

        public PrivilegeExecutionResult SetShellNewVisible(ContextMenuItem item, bool visible)
        {
            if (item == null || item.Kind != ContextMenuItemKind.ShellNew || string.IsNullOrEmpty(item.ClassName))
                throw new ArgumentException("The item must be a ShellNew entry.", nameof(item));

            var shellNewPath = RequireRegistryItem(item, ContextMenuItemKind.ShellNew);
            if (!string.Equals(item.ContainerRelativePath, "ShellNew", StringComparison.OrdinalIgnoreCase))
            {
                if (!RegistryPath.TryParse(item.ContainerRegistryPath, out RegistryPath shellNewParentPath))
                    throw new ArgumentException("The item does not contain a valid ShellNew parent path.", nameof(item));

                var moveResult = MoveKey(shellNewPath, shellNewParentPath.Append("ShellNew"));
                if (!moveResult.Succeeded)
                    return moveResult;
            }

            var classesPath = new RegistryPath(
                RegistryValueLocation.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew");
            var classes = (_registryReader.GetValue(classesPath, "Classes") as string[]) ?? [];
            var updatedClasses = classes.ToList();
            bool containsClass = updatedClasses.Contains(item.ClassName, StringComparer.OrdinalIgnoreCase);
            if (visible && !containsClass)
                updatedClasses.Add(item.ClassName);
            if (!visible && containsClass)
                updatedClasses.RemoveAll(name => string.Equals(name, item.ClassName, StringComparison.OrdinalIgnoreCase));

            return WriteValue(
                classesPath,
                "Classes",
                string.Join("\n", updatedClasses.ToArray()),
                RegistryValueKind.MultiString);
        }

        public PrivilegeExecutionResult SetShellNewText(ContextMenuItem item, string text)
        {
            var shellNewPath = RequireRegistryItem(item, ContextMenuItemKind.ShellNew);
            var result = DeleteValue(shellNewPath, "MenuText");
            if (!result.Succeeded)
                return result;

            var classPath = RegistryPath.ClassesRoot(item.ClassName);
            string defaultProgId = _registryReader.GetValue(classPath, null)?.ToString();
            if (string.IsNullOrEmpty(defaultProgId))
                return PrivilegeExecutionResult.Success();

            return WriteValue(
                RegistryPath.ClassesRoot(defaultProgId),
                "FriendlyTypeName",
                text ?? string.Empty,
                RegistryValueKind.String);
        }

        public PrivilegeExecutionResult SetShellNewIcon(ContextMenuItem item, string icon)
        {
            var shellNewPath = RequireRegistryItem(item, ContextMenuItemKind.ShellNew);
            return string.IsNullOrEmpty(icon)
                ? DeleteValue(shellNewPath, "IconPath")
                : WriteValue(shellNewPath, "IconPath", icon, RegistryValueKind.String);
        }

        public PrivilegeExecutionResult SetOpenWithText(ContextMenuItem item, string text)
        {
            var commandPath = RequireRegistryItem(item, ContextMenuItemKind.OpenWith);
            return WriteValue(commandPath.GetParent(), "FriendlyAppName", text ?? string.Empty, RegistryValueKind.String);
        }

        public PrivilegeExecutionResult DeleteRegistryBackedItem(ContextMenuItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Kind is not (ContextMenuItemKind.ShellCommand or ContextMenuItemKind.ShellExtension
                or ContextMenuItemKind.ShellNew or ContextMenuItemKind.OpenWith))
            {
                throw new ArgumentException("The item must be backed by a registry key.", nameof(item));
            }
            if (!RegistryPath.TryParse(item.RegistryPath, out RegistryPath path))
                throw new ArgumentException("The item does not contain a valid registry path.", nameof(item));

            return DeleteKey(path);
        }

        public PrivilegeExecutionResult SetFileBackedVisible(ContextMenuItem item, bool visible, bool elevated)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath))
                throw new ArgumentException("The item must contain a file path.", nameof(item));

            var attributes = File.GetAttributes(item.FilePath);
            attributes = visible ? attributes & ~FileAttributes.Hidden : attributes | FileAttributes.Hidden;
            return _fileOperator.SetAttributes(item.FilePath, attributes, elevated);
        }

        public PrivilegeExecutionResult SetShortcutText(ContextMenuItem item, string text, bool elevated)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath))
                throw new ArgumentException("The item must contain a shortcut file path.", nameof(item));

            using var link = new ShellLink(item.FilePath, writable: !elevated);
            link.Description = text ?? string.Empty;
            SaveShellLink(link, item.FilePath, elevated);
            return PrivilegeExecutionResult.Success();
        }

        public PrivilegeExecutionResult SetShortcutIcon(ContextMenuItem item, string icon, bool elevated)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath))
                throw new ArgumentException("The item must contain a shortcut file path.", nameof(item));

            using var link = new ShellLink(item.FilePath, writable: !elevated);
            link.IconLocation = ParseIconLocation(icon);
            SaveShellLink(link, item.FilePath, elevated);
            return PrivilegeExecutionResult.Success();
        }

        public PrivilegeExecutionResult SetSendToText(ContextMenuItem item, string text, bool elevated)
        {
            if (item == null || item.Kind != ContextMenuItemKind.SendTo || string.IsNullOrEmpty(item.FilePath))
                throw new ArgumentException("The item must be a SendTo entry.", nameof(item));

            var desktopIni = new DesktopIniFile(Path.GetDirectoryName(item.FilePath));
            string content = desktopIni.CreateContentWithValue(
                "LocalizedFileNames",
                Path.GetFileName(item.FilePath),
                text ?? string.Empty);
            return _fileOperator.WriteAllText(desktopIni.FilePath, content, elevated);
        }

        public PrivilegeExecutionResult RenameFileBackedItem(
            ContextMenuItem item,
            string destinationPath,
            bool elevated)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath))
                throw new ArgumentException("The item must contain a file path.", nameof(item));
            return _fileOperator.Rename(item.FilePath, destinationPath, elevated);
        }

        public PrivilegeExecutionResult DeleteFileBackedItem(
            ContextMenuItem item,
            bool recursive,
            bool elevated)
        {
            if (item == null || string.IsNullOrEmpty(item.FilePath))
                throw new ArgumentException("The item must contain a file path.", nameof(item));
            return _fileOperator.Delete(item.FilePath, recursive, elevated);
        }

        private PrivilegeExecutionResult MoveToPrimaryContainer(
            ContextMenuItem item,
            RegistryPath currentPath,
            string targetContainerRelativePath)
        {
            if (string.Equals(item.ContainerRelativePath, targetContainerRelativePath, StringComparison.OrdinalIgnoreCase))
                return PrivilegeExecutionResult.Success();
            if (!RegistryPath.TryParse(item.AssociationRegistryPath, out RegistryPath associationPath))
                throw new ArgumentException("The item does not contain a valid association registry path.", nameof(item));

            var destinationPath = associationPath.Append(targetContainerRelativePath).Append(item.KeyName);
            return MoveKey(currentPath, destinationPath);
        }

        private static RegistryPath GetPathInContainer(ContextMenuItem item, string containerRelativePath)
        {
            if (!RegistryPath.TryParse(item.AssociationRegistryPath, out RegistryPath associationPath))
                return null;
            return associationPath.Append(containerRelativePath).Append(item.KeyName);
        }

        private static RegistryPath RequireRegistryItem(ContextMenuItem item, ContextMenuItemKind expectedKind)
        {
            if (item == null || item.Kind != expectedKind)
                throw new ArgumentException("The item has an unexpected context menu kind.", nameof(item));
            if (!RegistryPath.TryParse(item.RegistryPath, out RegistryPath path))
                throw new ArgumentException("The item does not contain a valid registry path.", nameof(item));
            return path;
        }

        private PrivilegeExecutionResult SetShellCommandMarkerValue(
            ContextMenuItem item,
            string valueName,
            bool enabled)
        {
            var path = RequireRegistryItem(item, ContextMenuItemKind.ShellCommand);
            return enabled
                ? WriteValue(path, valueName, string.Empty, RegistryValueKind.String)
                : DeleteValue(path, valueName);
        }

        private PrivilegeExecutionResult DeleteCommandFlagsWhenHidden(RegistryPath path)
        {
            object value = _registryReader.GetValue(path, "CommandFlags");
            if (value == null)
                return PrivilegeExecutionResult.Success();

            if (TryConvertToInt32(value, out int commandFlags) && commandFlags % 16 >= 8)
                return DeleteValue(path, "CommandFlags");

            return PrivilegeExecutionResult.Success();
        }

        private static bool TryConvertToInt32(object value, out int result)
        {
            if (value is int intValue)
            {
                result = intValue;
                return true;
            }

            if (value is string stringValue)
                return int.TryParse(stringValue, out result);

            try
            {
                result = Convert.ToInt32(value);
                return true;
            }
            catch (FormatException)
            {
                result = 0;
                return false;
            }
            catch (InvalidCastException)
            {
                result = 0;
                return false;
            }
        }

        private void SaveShellLink(ShellLink link, string path, bool elevated)
        {
            if (elevated)
            {
                ShellLinkFileWriter.SaveWithElevation(link, path, _privilegeBroker);
                return;
            }

            link.Save();
        }

        private static (string FileName, int Index) ParseIconLocation(string icon)
        {
            if (StringCompat.IsNullOrWhiteSpace(icon))
                return (string.Empty, 0);

            string trimmed = icon.Trim();
            int commaIndex = trimmed.LastIndexOf(',');
            if (commaIndex <= 0)
                return (trimmed, 0);

            string path = trimmed.Substring(0, commaIndex);
            if (!int.TryParse(trimmed.Substring(commaIndex + 1), out int index))
                index = 0;
            return (path.Trim().Trim('"'), index);
        }

        private PrivilegeExecutionResult WriteValue(
            RegistryPath path,
            string valueName,
            string valueData,
            RegistryValueKind valueKind)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = path.Location,
                SubKeyPath = path.SubKeyPath,
                ValueName = valueName,
                ValueData = valueData,
                ValueKind = valueKind
            };

            if (path.Location == RegistryValueLocation.CurrentUser)
            {
                RegistryWriter.WriteValue(request);
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(ElevatedOperations.RegistryWrite, request);
        }

        private PrivilegeExecutionResult DeleteValue(RegistryPath path, string valueName)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = path.Location,
                SubKeyPath = path.SubKeyPath,
                ValueName = valueName,
                ValueKind = RegistryValueKind.String
            };

            if (path.Location == RegistryValueLocation.CurrentUser)
            {
                RegistryWriter.DeleteValue(request);
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(ElevatedOperations.RegistryDelete, request);
        }

        private PrivilegeExecutionResult MoveKey(RegistryPath sourcePath, RegistryPath destinationPath)
        {
            var request = new RegistryKeyOperationRequest
            {
                Location = sourcePath.Location,
                SourceSubKeyPath = sourcePath.SubKeyPath,
                DestinationSubKeyPath = destinationPath.SubKeyPath,
                Recursive = true
            };

            if (sourcePath.Location == RegistryValueLocation.CurrentUser)
            {
                RegistryWriter.MoveKey(request);
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(ElevatedOperations.RegistryMoveKey, request);
        }

        private PrivilegeExecutionResult DeleteKey(RegistryPath path)
        {
            var request = new RegistryKeyOperationRequest
            {
                Location = path.Location,
                SourceSubKeyPath = path.SubKeyPath,
                Recursive = true
            };

            if (path.Location == RegistryValueLocation.CurrentUser)
            {
                RegistryWriter.DeleteKey(request);
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(ElevatedOperations.RegistryDeleteKey, request);
        }

        private PrivilegeExecutionResult ExecuteElevated<T>(string operationName, T payload)
        {
            if (_privilegeBroker == null)
                return PrivilegeExecutionResult.Unavailable(
                    PrivilegeErrorCodes.ElevatedAgentUnavailable,
                    "The elevated agent controller is not available.");

            var request = new ElevatedCommandRequest
            {
                OperationName = operationName,
                Payload = DataContractPayloadSerializer.Serialize(payload),
                PrivilegeLevel = PrivilegeLevel.Administrator
            };
            return _privilegeBroker.Execute(request);
        }
    }
}
