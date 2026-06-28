using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinCraft.Compatibility;
using WinCraft.Infrastructure.FileSystem;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Shell.Shortcuts;

namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Enumerates context menu entries without requesting write access.
    /// </summary>
    internal sealed class ContextMenuCatalog
    {
        internal static readonly string[] ShellKeyNames =
        [
            "Shell",
            @"MenuMgr\Shell",
            @"ShellEx_Bak\Shell",
        ];

        internal static readonly string[] ContextMenuHandlersKeyNames =
        [
            @"ShellEx\ContextMenuHandlers",
            @"ShellEx\-ContextMenuHandlers",
            @"-ShellEx\ContextMenuHandlers",
            @"MenuMgr\ShellEx\ContextMenuHandlers",
            @"ShellEx_Bak\ShellEx\ContextMenuHandlers",
        ];

        internal static readonly string[] DragDropHandlersKeyNames =
        [
            @"ShellEx\DragDropHandlers",
            @"ShellEx\-DragDropHandlers",
        ];

        internal static readonly string[] ShellNewKeyNames =
        [
            "ShellNew",
            "-ShellNew",
            "ShellNew-",
        ];

        internal static readonly string[] ShellNewEffectValueNames =
        [
            "NullFile",
            "Data",
            "FileName",
            "Directory",
            "Command",
        ];

        private readonly IRegistryReader _registryReader;
        private readonly string _sendToPath;
        private readonly string _winXPath;

        public ContextMenuCatalog()
            : this(
                new RegistryReader(),
                Environment.ExpandEnvironmentVariables(@"%AppData%\Microsoft\Windows\SendTo"),
                Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\WinX"))
        {
        }

        internal ContextMenuCatalog(IRegistryReader registryReader, string sendToPath, string winXPath)
        {
            _registryReader = registryReader ?? throw new ArgumentNullException(nameof(registryReader));
            _sendToPath = sendToPath;
            _winXPath = winXPath;
        }

        public List<ContextMenuItem> Load(ContextMenuScope scope)
        {
            return scope switch
            {
                ContextMenuScope.CommandStore => LoadCommandStoreItems(),
                ContextMenuScope.ShellNew => LoadShellNewItems(),
                ContextMenuScope.SendTo => LoadSendToItems(),
                ContextMenuScope.OpenWith => LoadOpenWithItems(),
                ContextMenuScope.WinX => LoadWinXItems(),
                ContextMenuScope.None => [],
                _ => LoadAssociationItems(scope),
            };
        }

        public List<ContextMenuItem> LoadForExtension(string extension)
        {
            if (StringCompat.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("The extension is required.", nameof(extension));

            string normalizedExtension = extension.StartsWith(".", StringComparison.Ordinal)
                ? extension
                : "." + extension;
            var associations = new List<ContextMenuAssociation>
            {
                new(
                    ContextMenuAssociationKind.SystemAssoc,
                    RegistryPath.ClassesRoot(@"SystemFileAssociations\" + normalizedExtension))
            };

            string progId = GetValueString(RegistryPath.ClassesRoot(normalizedExtension), null);
            if (!string.IsNullOrEmpty(progId))
            {
                associations.Add(new ContextMenuAssociation(
                    ContextMenuAssociationKind.ProgramAssoc,
                    RegistryPath.ClassesRoot(progId)));
            }

            string perceivedType = GetValueString(RegistryPath.ClassesRoot(normalizedExtension), "PerceivedType")
                ?? GetValueString(RegistryPath.ClassesRoot(@"SystemFileAssociations\" + normalizedExtension), "PerceivedType");
            if (TryMapPerceivedType(perceivedType, out ContextMenuAssociationKind perceivedKind))
            {
                associations.Add(new ContextMenuAssociation(
                    perceivedKind,
                    RegistryPath.ClassesRoot(@"SystemFileAssociations\" + perceivedType)));
            }

            return LoadAssociationItems(associations, isDragDrop: false);
        }

        public List<ContextMenuItem> LoadSubItems(ContextMenuItem item)
        {
            if (item == null || item.Kind != ContextMenuItemKind.ShellCommand)
                throw new ArgumentException("The item must be a shell command entry.", nameof(item));
            if (!RegistryPath.TryParse(item.RegistryPath, out RegistryPath itemPath))
                throw new ArgumentException("The item does not contain a valid registry path.", nameof(item));

            var items = new List<ContextMenuItem>();
            string subCommands = GetValueString(itemPath, "SubCommands");
            if (subCommands != null && subCommands.Length == 0)
            {
                var shellPath = itemPath.Append("shell");
                foreach (string itemName in _registryReader.GetSubKeyNames(shellPath))
                    items.Add(CreateShellCommandItem(
                        new ContextMenuAssociation(item.AssociationKind, itemPath),
                        shellPath.Append(itemName),
                        "shell",
                        isSubItem: true));
            }

            string extendedSubCommandsKey = GetValueString(itemPath, "ExtendedSubCommandsKey")?.Trim('\\');
            if (!string.IsNullOrEmpty(extendedSubCommandsKey))
            {
                var shellPath = RegistryPath.ClassesRoot(extendedSubCommandsKey).Append("shell");
                foreach (string itemName in _registryReader.GetSubKeyNames(shellPath))
                    items.Add(CreateShellCommandItem(
                        new ContextMenuAssociation(item.AssociationKind, RegistryPath.ClassesRoot(extendedSubCommandsKey)),
                        shellPath.Append(itemName),
                        "shell",
                        isSubItem: true));
            }

            return items;
        }

        private List<ContextMenuItem> LoadAssociationItems(ContextMenuScope scope)
        {
            bool isDragDrop = scope == ContextMenuScope.DragDrop;
            return LoadAssociationItems(ContextMenuAssociationPlanner.GetAssociations(scope), isDragDrop);
        }

        private List<ContextMenuItem> LoadAssociationItems(
            IList<ContextMenuAssociation> associations,
            bool isDragDrop)
        {
            var items = new List<ContextMenuItem>();
            foreach (var association in associations)
            {
                if (!isDragDrop)
                    LoadShellCommandItems(association, items);
                LoadShellExtensionItems(association, isDragDrop, items);
            }

            return items;
        }

        private void LoadShellCommandItems(ContextMenuAssociation association, List<ContextMenuItem> items)
        {
            foreach (string shellKeyName in ShellKeyNames)
            {
                var shellPath = association.RegistryPath.Append(shellKeyName);
                foreach (string itemName in _registryReader.GetSubKeyNames(shellPath))
                {
                    var itemPath = shellPath.Append(itemName);
                    items.Add(CreateShellCommandItem(association, itemPath, shellKeyName, isSubItem: false));
                }
            }
        }

        private void LoadShellExtensionItems(
            ContextMenuAssociation association,
            bool isDragDrop,
            List<ContextMenuItem> items)
        {
            var handlerKeyNames = isDragDrop ? DragDropHandlersKeyNames : ContextMenuHandlersKeyNames;
            foreach (string handlersKeyName in handlerKeyNames)
            {
                var handlersPath = association.RegistryPath.Append(handlersKeyName);
                foreach (string itemName in _registryReader.GetSubKeyNames(handlersPath))
                {
                    var itemPath = handlersPath.Append(itemName);
                    string clsid = TryNormalizeClsid(itemName) ?? TryNormalizeClsid(GetValueString(itemPath, null));
                    items.Add(new ContextMenuItem
                    {
                        Kind = ContextMenuItemKind.ShellExtension,
                        AssociationKind = association.Kind,
                        AssociationRegistryPath = association.RegistryPath.ToString(),
                        ContainerRegistryPath = handlersPath.ToString(),
                        ContainerRelativePath = handlersKeyName,
                        RegistryPath = itemPath.ToString(),
                        KeyName = itemName,
                        Text = string.IsNullOrEmpty(clsid) ? itemName : clsid,
                        FilePath = GetClsidServerPath(clsid),
                        Visible = handlersKeyName == handlerKeyNames[0] && !string.IsNullOrEmpty(clsid),
                        IsDragDrop = isDragDrop
                    });
                }
            }
        }

        private List<ContextMenuItem> LoadCommandStoreItems()
        {
            var items = new List<ContextMenuItem>();
            LoadCommandStoreItems(
                ContextMenuAssociationKind.SystemCommand,
                new RegistryPath(RegistryValueLocation.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\Shell"),
                items);
            LoadCommandStoreItems(
                ContextMenuAssociationKind.UserCommand,
                new RegistryPath(RegistryValueLocation.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\Shell"),
                items);
            return items;
        }

        private void LoadCommandStoreItems(
            ContextMenuAssociationKind associationKind,
            RegistryPath shellPath,
            List<ContextMenuItem> items)
        {
            var association = new ContextMenuAssociation(associationKind, shellPath.GetParent());
            foreach (string itemName in _registryReader.GetSubKeyNames(shellPath))
                items.Add(CreateShellCommandItem(association, shellPath.Append(itemName), "Shell", isSubItem: false));
        }

        private List<ContextMenuItem> LoadShellNewItems()
        {
            var items = new List<ContextMenuItem>();
            foreach (string className in _registryReader.GetSubKeyNames(RegistryPath.ClassesRoot(string.Empty)))
            {
                if (!className.Equals("Folder", StringComparison.OrdinalIgnoreCase)
                    && !className.StartsWith(".", StringComparison.Ordinal))
                {
                    continue;
                }

                var classPath = RegistryPath.ClassesRoot(className);
                string defaultProgId = GetValueString(classPath, null);
                var shellNewParentPath = !string.IsNullOrEmpty(defaultProgId)
                    && _registryReader.KeyExists(classPath.Append(defaultProgId))
                        ? classPath.Append(defaultProgId)
                        : classPath;

                foreach (string shellNewKeyName in ShellNewKeyNames)
                {
                    var shellNewPath = shellNewParentPath.Append(shellNewKeyName);
                    if (!ContainsAnyValue(shellNewPath, ShellNewEffectValueNames))
                        continue;

                    string text = GetShellNewText(classPath, shellNewPath);
                    if (string.IsNullOrEmpty(text))
                        break;

                    items.Add(new ContextMenuItem
                    {
                        Kind = ContextMenuItemKind.ShellNew,
                        AssociationKind = ContextMenuAssociationKind.ShellNew,
                        RegistryPath = shellNewPath.ToString(),
                        ContainerRegistryPath = shellNewParentPath.ToString(),
                        ContainerRelativePath = shellNewKeyName,
                        KeyName = shellNewKeyName,
                        ClassName = className,
                        Text = text,
                        Icon = GetValueString(shellNewPath, "IconPath"),
                        Visible = shellNewKeyName == ShellNewKeyNames[0] && IsShellNewClassEnabled(className)
                    });
                    break;
                }
            }

            return items;
        }

        private List<ContextMenuItem> LoadSendToItems()
        {
            var items = new List<ContextMenuItem>();
            if (string.IsNullOrEmpty(_sendToPath) || !Directory.Exists(_sendToPath))
                return items;

            foreach (string filePath in Directory.GetFileSystemEntries(_sendToPath))
            {
                if (Path.GetFileName(filePath).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                    continue;

                items.Add(CreateFileBackedItem(ContextMenuItemKind.SendTo, ContextMenuAssociationKind.SendTo, filePath));
            }

            return items.OrderBy(item => item.Text, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private List<ContextMenuItem> LoadOpenWithItems()
        {
            var items = new List<ContextMenuItem>();
            var applicationsPath = RegistryPath.ClassesRoot("Applications");
            foreach (string appName in _registryReader.GetSubKeyNames(applicationsPath))
            {
                var appPath = applicationsPath.Append(appName);
                var shellPath = appPath.Append("shell");
                foreach (string verbName in _registryReader.GetSubKeyNames(shellPath)
                    .OrderBy(name => name.Equals("open", StringComparison.OrdinalIgnoreCase)))
                {
                    var verbPath = shellPath.Append(verbName);
                    if (_registryReader.GetValue(verbPath, "NeverDefault") != null)
                        continue;

                    var commandPath = verbPath.Append("command");
                    string command = GetValueString(commandPath, null);
                    if (string.IsNullOrEmpty(command))
                        continue;
                    string filePath = ShellCommandTargetParser.GetExecutablePath(command);
                    if (!IsOpenWithCommandForApplication(appName, filePath))
                        continue;

                    items.Add(new ContextMenuItem
                    {
                        Kind = ContextMenuItemKind.OpenWith,
                        AssociationKind = ContextMenuAssociationKind.OpenWith,
                        RegistryPath = commandPath.ToString(),
                        ContainerRegistryPath = appPath.ToString(),
                        ContainerRelativePath = "Applications",
                        KeyName = appName,
                        Text = GetValueString(verbPath, "FriendlyAppName")
                            ?? GetValueString(appPath, "FriendlyAppName")
                            ?? appName,
                        Command = command,
                        FilePath = filePath,
                        Visible = _registryReader.GetValue(appPath, "NoOpenWith") == null
                    });
                    break;
                }
            }

            return items.OrderBy(item => item.Text, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private List<ContextMenuItem> LoadWinXItems()
        {
            var items = new List<ContextMenuItem>();
            if (string.IsNullOrEmpty(_winXPath) || !Directory.Exists(_winXPath))
                return items;

            foreach (string directoryPath in Directory.GetDirectories(_winXPath).Reverse())
            {
                foreach (string filePath in Directory.GetFiles(directoryPath, "*.lnk").Reverse())
                    items.Add(CreateFileBackedItem(ContextMenuItemKind.WinX, ContextMenuAssociationKind.WinX, filePath));
            }

            return items;
        }

        private ContextMenuItem CreateShellCommandItem(
            ContextMenuAssociation association,
            RegistryPath itemPath,
            string shellKeyName,
            bool isSubItem)
        {
            string keyName = itemPath.GetName();
            string command = GetValueString(itemPath.Append("Command"), null);
            string handlerClsid = GetShellCommandClsid(itemPath);
            string clsidServerPath = GetClsidServerPath(handlerClsid);
            string icon = GetValueString(itemPath, "Icon");
            string text = GetValueString(itemPath, "MUIVerb")
                ?? GetValueString(itemPath, null)
                ?? GetClsidDisplayName(handlerClsid)
                ?? keyName;

            return new ContextMenuItem
            {
                Kind = ContextMenuItemKind.ShellCommand,
                AssociationKind = association.Kind,
                AssociationRegistryPath = association.RegistryPath.ToString(),
                ContainerRegistryPath = itemPath.GetParent().ToString(),
                ContainerRelativePath = shellKeyName,
                RegistryPath = itemPath.ToString(),
                KeyName = keyName,
                Text = text,
                Icon = icon ?? (HasValue(itemPath, "HasLUAShield") ? "imageres.dll,-78" : GetClsidIcon(handlerClsid)),
                Command = command,
                Position = GetPosition(itemPath),
                ShowIcon = icon != null || HasValue(itemPath, "HasLUAShield"),
                HasLuaShield = icon == null && HasValue(itemPath, "HasLUAShield"),
                OnlyWithShift = HasValue(itemPath, "Extended"),
                OnlyInExplorer = HasValue(itemPath, "OnlyInBrowserWindow"),
                NoWorkingDirectory = HasValue(itemPath, "NoWorkingDirectory"),
                NeverDefault = HasValue(itemPath, "NeverDefault"),
                FilePath = clsidServerPath ?? ShellCommandTargetParser.GetExecutablePath(command),
                Visible = IsShellCommandVisible(itemPath, shellKeyName, isSubItem),
                IsSubItem = isSubItem,
                HasSubItems = _registryReader.GetValue(itemPath, "SubCommands") != null
                    || !string.IsNullOrEmpty(GetValueString(itemPath, "ExtendedSubCommandsKey"))
            };
        }

        private ContextMenuItem CreateFileBackedItem(
            ContextMenuItemKind kind,
            ContextMenuAssociationKind associationKind,
            string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            var desktopIni = new DesktopIniFile(Path.GetDirectoryName(filePath));
            string localizedText = desktopIni.GetValue("LocalizedFileNames", fileName);
            string text = !string.IsNullOrEmpty(localizedText)
                ? localizedText
                : Path.GetFileNameWithoutExtension(filePath);
            string targetPath = filePath;
            string icon = filePath;

            if (Path.GetExtension(filePath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                using var link = new ShellLink(filePath);
                if (kind == ContextMenuItemKind.WinX && !StringCompat.IsNullOrWhiteSpace(link.Description))
                    text = link.Description;
                targetPath = link.TargetPath;
                var iconLocation = link.IconLocation;
                if (!StringCompat.IsNullOrWhiteSpace(iconLocation.FileName))
                    icon = iconLocation.FileName + "," + iconLocation.Index;
            }

            return new ContextMenuItem
            {
                Kind = kind,
                AssociationKind = associationKind,
                KeyName = Path.GetFileName(filePath),
                Text = string.IsNullOrEmpty(text) ? Path.GetFileName(filePath) : text,
                Icon = icon,
                FilePath = filePath,
                TargetPath = targetPath,
                GroupName = Path.GetFileName(Path.GetDirectoryName(filePath)),
                Visible = (File.GetAttributes(filePath) & FileAttributes.Hidden) == 0
            };
        }

        private bool IsShellCommandVisible(RegistryPath itemPath, string shellKeyName, bool isSubItem)
        {
            if (shellKeyName != ShellKeyNames[0])
                return false;
            if (!isSubItem)
            {
                if (_registryReader.GetValue(itemPath, "LegacyDisable") != null)
                    return false;
                if (_registryReader.GetValue(itemPath, "ProgrammaticAccessOnly") != null)
                    return false;
                if (GetIntegerValue(itemPath, "CommandFlags") % 16 >= 8)
                    return false;
            }

            object velocityId = _registryReader.GetValue(itemPath, "HideBasedOnVelocityId");
            return !object.Equals(velocityId, 0x639bc8);
        }

        private bool ContainsAnyValue(RegistryPath path, string[] valueNames)
        {
            var existingValueNames = new HashSet<string>(_registryReader.GetValueNames(path), StringComparer.OrdinalIgnoreCase);
            return valueNames.Any(existingValueNames.Contains);
        }

        private string GetShellNewText(RegistryPath classPath, RegistryPath shellNewPath)
        {
            string defaultProgId = GetValueString(classPath, null);
            if (string.IsNullOrEmpty(defaultProgId))
                return null;

            var defaultProgIdPath = RegistryPath.ClassesRoot(defaultProgId);
            string fallback = GetValueString(defaultProgIdPath, "FriendlyTypeName")
                ?? GetValueString(defaultProgIdPath, null);
            if (string.IsNullOrEmpty(fallback))
                return null;

            string menuText = GetValueString(shellNewPath, "MenuText");
            return !string.IsNullOrEmpty(menuText) ? menuText : fallback;
        }

        private bool IsShellNewClassEnabled(string className)
        {
            return _registryReader.GetValue(
                new RegistryPath(
                    RegistryValueLocation.CurrentUser,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew"),
                    "Classes") 
                is string[] value 
                && value.Contains(className, StringComparer.OrdinalIgnoreCase);
        }

        private string GetClsidServerPath(string clsid)
        {
            if (string.IsNullOrEmpty(clsid))
                return null;

            foreach (string clsidRoot in new[] { "CLSID", @"WOW6432Node\CLSID" })
            {
                var clsidPath = RegistryPath.ClassesRoot(clsidRoot).Append(clsid);
                string inprocServer = GetValueString(clsidPath.Append("InprocServer32"), null);
                if (!string.IsNullOrEmpty(inprocServer))
                    return GetClsidInprocPath(clsidPath, inprocServer);

                string localServer = GetValueString(clsidPath.Append("LocalServer32"), "ServerExecutable");
                if (!string.IsNullOrEmpty(localServer))
                    return localServer;

                localServer = GetValueString(clsidPath.Append("LocalServer32"), null);
                if (!string.IsNullOrEmpty(localServer))
                    return ShellCommandTargetParser.GetExecutablePath(localServer) ?? localServer;
            }

            return null;
        }

        private string GetClsidInprocPath(RegistryPath clsidPath, string inprocServer)
        {
            if (inprocServer.EndsWith("mscoree.dll", StringComparison.OrdinalIgnoreCase))
            {
                string codeBase = GetValueString(clsidPath.Append("InprocServer32"), "CodeBase");
                if (!string.IsNullOrEmpty(codeBase) && Uri.TryCreate(codeBase, UriKind.Absolute, out Uri uri) && uri.IsFile)
                    return uri.LocalPath;
            }

            return inprocServer;
        }

        private string GetShellCommandClsid(RegistryPath itemPath)
        {
            return TryNormalizeClsid(GetValueString(itemPath.Append("Command"), "DelegateExecute"))
                ?? TryNormalizeClsid(GetValueString(itemPath.Append("DropTarget"), "CLSID"))
                ?? TryNormalizeClsid(GetValueString(itemPath, "ExplorerCommandHandler"));
        }

        private string GetClsidDisplayName(string clsid)
        {
            if (string.IsNullOrEmpty(clsid))
                return null;

            var clsidPath = RegistryPath.ClassesRoot("CLSID").Append(clsid);
            return GetValueString(clsidPath, "LocalizedString")
                ?? GetValueString(clsidPath, "InfoTip")
                ?? GetValueString(clsidPath, null);
        }

        private string GetClsidIcon(string clsid)
        {
            if (string.IsNullOrEmpty(clsid))
                return null;

            return GetValueString(RegistryPath.ClassesRoot("CLSID").Append(clsid).Append("DefaultIcon"), null);
        }

        private string GetValueString(RegistryPath path, string valueName)
        {
            return _registryReader.GetValue(path, valueName)?.ToString();
        }

        private static bool IsOpenWithCommandForApplication(string appName, string filePath)
        {
            if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(filePath))
                return false;

            return string.Equals(Path.GetFileName(filePath), appName, StringComparison.OrdinalIgnoreCase);
        }

        private bool HasValue(RegistryPath path, string valueName)
        {
            return _registryReader.GetValue(path, valueName) != null;
        }

        private ContextMenuCommandPosition GetPosition(RegistryPath itemPath)
        {
            string value = GetValueString(itemPath, "Position");
            if (EnumCompat.TryParse(value, true, out ContextMenuCommandPosition position))
                return position;
            return ContextMenuCommandPosition.Default;
        }

        private int GetIntegerValue(RegistryPath path, string valueName)
        {
            object value = _registryReader.GetValue(path, valueName);
            if (value == null)
                return 0;
            try
            {
                return Convert.ToInt32(value);
            }
            catch (FormatException)
            {
                return 0;
            }
            catch (InvalidCastException)
            {
                return 0;
            }
        }

        private static string TryNormalizeClsid(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            string trimmed = value.Trim();
            if (trimmed.Length == 39 && trimmed.EndsWith("-}", StringComparison.Ordinal))
                trimmed = "{" + trimmed.Substring(1, 36) + "}";
            return TryParseGuid(trimmed, out Guid clsid) ? clsid.ToString("B") : null;
        }

        private static bool TryParseGuid(string input, out Guid result)
        {
            return GuidCompat.TryParse(input, out result);
        }

        private static readonly Dictionary<string, ContextMenuAssociationKind> PerceivedTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Image", ContextMenuAssociationKind.Image },
            { "Audio", ContextMenuAssociationKind.Audio },
            { "Video", ContextMenuAssociationKind.Video },
            { "Text", ContextMenuAssociationKind.Text },
            { "Document", ContextMenuAssociationKind.Document },
            { "Compressed", ContextMenuAssociationKind.Compressed },
            { "System", ContextMenuAssociationKind.System },
        };

        private static bool TryMapPerceivedType(string perceivedType, out ContextMenuAssociationKind associationKind)
        {
            if (PerceivedTypeMap.TryGetValue(perceivedType, out associationKind))
                return true;

            associationKind = ContextMenuAssociationKind.Unknown;
            return false;
        }
    }
}
