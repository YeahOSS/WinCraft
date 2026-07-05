using System;
using System.Collections.Generic;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Tests.Infrastructure.RegistryAccess
{
    internal sealed class FakeRegistryReader : IRegistryReader
    {
        private readonly Dictionary<string, Dictionary<string, object>> _keys =
            new(StringComparer.OrdinalIgnoreCase);

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
                return [];

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
