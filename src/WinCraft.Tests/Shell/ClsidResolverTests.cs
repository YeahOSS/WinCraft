using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Shell;
using WinCraft.Tests.Infrastructure.RegistryAccess;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    internal sealed class ClsidResolverTests
    {
        private const string KnownClsid = "{00000000-0000-0000-0000-000000000001}";

        [Test]
        public void Normalize_StandardGuid_ReturnsBracedFormat()
        {
            var result = ClsidResolver.Normalize("{00000000-0000-0000-0000-000000000001}");

            Assert.That(result, Is.EqualTo(KnownClsid));
        }

        [Test]
        public void Normalize_GuidWithoutBraces_ReturnsBracedFormat()
        {
            var result = ClsidResolver.Normalize("00000000-0000-0000-0000-000000000001");

            Assert.That(result, Is.EqualTo(KnownClsid));
        }

        [Test]
        public void Normalize_CsWin32BareFormat_ReturnsBracedFormat()
        {
            // CsWin32-generated CLSID strings omit the leading { and trailing }-.
            // e.g. "00000000-0000-0000-0000-000000000001}-"
            var result = ClsidResolver.Normalize("{00000000-0000-0000-0000-000000000001}-");

            Assert.That(result, Is.EqualTo(KnownClsid));
        }

        [Test]
        public void Normalize_NotAGuid_ReturnsNull()
        {
            var result = ClsidResolver.Normalize("not-a-guid");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Normalize_Null_ReturnsNull()
        {
            var result = ClsidResolver.Normalize(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Normalize_Empty_ReturnsNull()
        {
            var result = ClsidResolver.Normalize(string.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Normalize_LeadingTrailingWhitespace_TrimsAndParses()
        {
            var result = ClsidResolver.Normalize("  {00000000-0000-0000-0000-000000000001}  ");

            Assert.That(result, Is.EqualTo(KnownClsid));
        }

        [Test]
        public void Resolve_Null_ReturnsNull()
        {
            var resolver = new ClsidResolver(CreateRegistry());

            var result = resolver.Resolve(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Resolve_NotFound_ReturnsNull()
        {
            var resolver = new ClsidResolver(CreateRegistry());

            var result = resolver.Resolve("{00000000-0000-0000-0000-000000000099}");

            Assert.That(result, Is.Null); // not in registry → ResolveServerPath returns null; ResolveDisplayName + ResolveIcon still work
        }

        [Test]
        public void Resolve_KnownClsidWithDisplayName_ReturnsResolvedInfo()
        {
            var registry = CreateRegistry();
            registry.SetValue(@"HKEY_CLASSES_ROOT\CLSID\" + KnownClsid, "LocalizedString", "Test Handler");
            registry.SetValue(@"HKEY_CLASSES_ROOT\CLSID\" + KnownClsid + @"\DefaultIcon", null, "handler.dll,3");
            var resolver = new ClsidResolver(registry);

            var result = resolver.Resolve(KnownClsid);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Value.Clsid, Is.EqualTo(KnownClsid));
            Assert.That(result.Value.Text, Is.EqualTo("Test Handler"));
            Assert.That(result.Value.Icon, Is.EqualTo(new IconLocation("handler.dll,3")));
        }

        [Test]
        public void ResolveServerPath_InprocServer32_WinsOverLocalServer()
        {
            var registry = CreateRegistry();
            registry.SetValue(
                @"HKEY_CLASSES_ROOT\CLSID\" + KnownClsid + @"\InprocServer32",
                null,
                @"C:\Tools\handler.dll");
            registry.SetValue(
                @"HKEY_CLASSES_ROOT\CLSID\" + KnownClsid + @"\LocalServer32",
                null,
                @"C:\Tools\server.exe");
            var resolver = new ClsidResolver(registry);

            var result = resolver.Resolve(KnownClsid);

            Assert.That(result.Value.FilePath, Is.EqualTo(@"C:\Tools\handler.dll"));
        }

        [Test]
        public void ResolveServerPath_LocalServer32_ServerExecutable_NamedValue_WinsOverDefault()
        {
            var registry = CreateRegistry();
            registry.SetValue(
                @"HKEY_CLASSES_ROOT\CLSID\" + KnownClsid + @"\LocalServer32",
                "ServerExecutable",
                @"C:\Tools\server.exe");
            registry.SetValue(
                @"HKEY_CLASSES_ROOT\CLSID\" + KnownClsid + @"\LocalServer32",
                null,
                @"C:\Tools\wrong.exe /arg");
            var resolver = new ClsidResolver(registry);

            var result = resolver.Resolve(KnownClsid);

            Assert.That(result.Value.FilePath, Is.EqualTo(@"C:\Tools\server.exe"));
        }

        [Test]
        public void ResolveServerPath_LocalServer32_DefaultWithArgs_ExtractsExecutablePath()
        {
            var registry = CreateRegistry();
            registry.SetValue(
                @"HKEY_CLASSES_ROOT\CLSID\" + KnownClsid + @"\LocalServer32",
                null,
                @"""C:\Tools\server.exe"" /Automation");
            var resolver = new ClsidResolver(registry);

            var result = resolver.Resolve(KnownClsid);

            Assert.That(result.Value.FilePath, Is.EqualTo(@"C:\Tools\server.exe"));
        }

        [Test]
        public void ResolveServerPath_FallsBackToWow6432Node()
        {
            var registry = CreateRegistry();
            registry.SetValue(
                @"HKEY_CLASSES_ROOT\WOW6432Node\CLSID\" + KnownClsid + @"\InprocServer32",
                null,
                @"C:\SysWOW64\handler.dll");
            var resolver = new ClsidResolver(registry);

            var result = resolver.Resolve(KnownClsid);

            Assert.That(result.Value.FilePath, Is.EqualTo(@"C:\SysWOW64\handler.dll"));
        }

        [Test]
        public void ResolveIcon_MissingDefaultIcon_ReturnsDefault()
        {
            var registry = CreateRegistry();
            var resolver = new ClsidResolver(registry);

            var result = resolver.Resolve(KnownClsid);

            Assert.That(result.Value.Icon.FileName, Is.Empty);
            Assert.That(result.Value.Icon.Index, Is.EqualTo(0));
        }

        private static FakeRegistryReader CreateRegistry()
        {
            var registry = new FakeRegistryReader();
            registry.AddKey(@"HKEY_CLASSES_ROOT\CLSID");
            registry.AddKey(@"HKEY_CLASSES_ROOT\CLSID\" + KnownClsid);
            registry.AddKey(@"HKEY_CLASSES_ROOT\WOW6432Node\CLSID");
            return registry;
        }
    }
}
