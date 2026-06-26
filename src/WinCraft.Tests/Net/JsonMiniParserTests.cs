using System.Collections.Generic;
using NUnit.Framework;
using WinCraft.Infrastructure.Net;

namespace WinCraft.Tests.Net
{
    [TestFixture]
    internal sealed class JsonMiniParserTests
    {
        [Test]
        public void Parse_EmptyObject_ReturnsFreshDictionary()
        {
            var first = (Dictionary<string, object>)JsonMiniParser.Parse("{}");
            first["polluted"] = true;

            var second = (Dictionary<string, object>)JsonMiniParser.Parse("{}");

            Assert.That(second, Is.Empty);
        }
    }
}
