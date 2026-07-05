using System.Runtime.InteropServices.ComTypes;
using NUnit.Framework;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    internal sealed class ShellDataObjectTests
    {
        private const string UnicodeTextFormat = "UnicodeText";

        [Test]
        public void Construct_DoesNotThrow()
        {
            Assert.That(() => new ShellDataObject(), Throws.Nothing);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var data = new ShellDataObject();

            Assert.That(() => data.Dispose(), Throws.Nothing);
        }

        [Test]
        public void Dispose_CanBeCalledTwice()
        {
            var data = new ShellDataObject();
            data.Dispose();

            Assert.That(() => data.Dispose(), Throws.Nothing);
        }

        [Test]
        public void SetText_StoresUnderUnicodeTextFormat()
        {
            using var data = new ShellDataObject();
            data.SetText("hello");

            var stream = ((IDataObject)data).GetStream(UnicodeTextFormat);
            Assert.That(stream, Is.Not.Null);

            using (stream)
            using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.Unicode))
            {
                var text = reader.ReadToEnd().TrimEnd('\0');
                Assert.That(text, Is.EqualTo("hello"));
            }
        }

        [Test]
        public void SetText_EmptyString_RoundTrips()
        {
            using var data = new ShellDataObject();
            data.SetText(string.Empty);

            var stream = ((IDataObject)data).GetStream(UnicodeTextFormat);
            Assert.That(stream, Is.Not.Null);

            using (stream)
            using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.Unicode))
            {
                var text = reader.ReadToEnd().TrimEnd('\0');
                Assert.That(text, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void EnumFormatEtc_GetDirection_ReturnsFormats()
        {
            using var data = new ShellDataObject();
            data.SetText("sample");

            var enumerator = data.EnumFormatEtc(DATADIR.DATADIR_GET);
            Assert.That(enumerator, Is.Not.Null);

            var formats = new FORMATETC[1];
            var fetched = new int[1];
            int hr = enumerator.Next(1, formats, fetched);
            Assert.That(hr, Is.EqualTo(0)); // S_OK
            Assert.That(fetched[0], Is.EqualTo(1));
        }

        [Test]
        public void QueryGetData_StoredFormat_ReturnsOK()
        {
            using var data = new ShellDataObject();
            data.SetText("query-test");

            var fmt = DataObjectExtensions.CreateFormatEtc(UnicodeTextFormat);
            var hr = ((IDataObject)data).QueryGetData(ref fmt);

            Assert.That(hr, Is.EqualTo(0)); // S_OK
        }

        [Test]
        public void QueryGetData_UnstoredFormat_ReturnsFormatEtcError()
        {
            using var data = new ShellDataObject();

            var fmt = DataObjectExtensions.CreateFormatEtc(UnicodeTextFormat);

            Assert.That(((IDataObject)data).QueryGetData(ref fmt), Is.LessThan(0));
        }

        [Test]
        public void GetData_MissingFormat_ReturnsNullMedium()
        {
            using var data = new ShellDataObject();
            var fmt = DataObjectExtensions.CreateFormatEtc("UnknownFormat");
            STGMEDIUM medium;
            ((IDataObject)data).GetData(ref fmt, out medium);

            Assert.That(medium.tymed, Is.EqualTo(TYMED.TYMED_NULL));
            Assert.That(medium.unionmember, Is.EqualTo(System.IntPtr.Zero));
        }
    }
}
