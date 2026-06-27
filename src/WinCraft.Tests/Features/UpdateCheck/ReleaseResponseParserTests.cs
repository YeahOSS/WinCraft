using System;
using NUnit.Framework;
using WinCraft.Features.UpdateCheck;

namespace WinCraft.Tests.Features.UpdateCheck
{
    [TestFixture]
    internal sealed class ReleaseResponseParserTests
    {
        [Test]
        public void Parse_EmptyJsonObject_ReturnsReleaseInfoWithDefaults()
        {
            var info = ReleaseResponseParser.Parse("{}");

            Assert.That(info, Is.Not.Null);
            Assert.That(info.Version, Is.EqualTo(new Version()));
            Assert.That(info.TagName, Is.Null);
            Assert.That(info.PublishedAt, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void Parse_InvalidJson_ThrowsFormatException()
        {
            Assert.That(
                () => ReleaseResponseParser.Parse("not json"),
                Throws.InstanceOf<FormatException>());
        }

        [Test]
        public void Parse_WithTagName_ParsesVersion()
        {
            var json = @"{""tag_name"": ""v1.2.3""}";

            var info = ReleaseResponseParser.Parse(json);

            Assert.That(info.TagName, Is.EqualTo("v1.2.3"));
            Assert.That(info.Version, Is.EqualTo(new Version(1, 2, 3)));
        }

        [Test]
        public void Parse_WithTagNameNoV_ParsesVersion()
        {
            var json = @"{""tag_name"": ""2.0.0""}";

            var info = ReleaseResponseParser.Parse(json);

            Assert.That(info.Version, Is.EqualTo(new Version(2, 0, 0)));
        }

        [Test]
        public void Parse_WithPublishedAt_ParsesDate()
        {
            var json = @"{""published_at"": ""2026-01-15T12:00:00Z""}";

            var info = ReleaseResponseParser.Parse(json);

            Assert.That(info.PublishedAt.Year, Is.EqualTo(2026));
            Assert.That(info.PublishedAt.Month, Is.EqualTo(1));
            Assert.That(info.PublishedAt.Day, Is.EqualTo(15));
        }

        [Test]
        public void Parse_WithCreatedAtFallback_ParsesDate()
        {
            var json = @"{""created_at"": ""2025-06-01T00:00:00+08:00""}";

            var info = ReleaseResponseParser.Parse(json);

            Assert.That(info.PublishedAt.Year, Is.EqualTo(2025));
        }

        [Test]
        public void Parse_WithAssets_SelectsExeAsset()
        {
            var json = @"{
                ""tag_name"": ""v1.0.0"",
                ""assets"": [
                    { ""name"": ""setup.exe"", ""browser_download_url"": ""https://example.com/setup.exe"" },
                    { ""name"": ""readme.txt"", ""browser_download_url"": ""https://example.com/readme.txt"" }
                ]
            }";

            var info = ReleaseResponseParser.Parse(json);

            Assert.That(info.AssetName, Is.EqualTo("setup.exe"));
            Assert.That(info.DownloadUrl, Is.EqualTo("https://example.com/setup.exe"));
        }

        [Test]
        public void Parse_WithAssetsNoExe_SetsNullDownloadUrl()
        {
            var json = @"{
                ""tag_name"": ""v1.0.0"",
                ""assets"": [
                    { ""name"": ""readme.txt"", ""browser_download_url"": ""https://example.com/readme.txt"" }
                ]
            }";

            var info = ReleaseResponseParser.Parse(json);

            Assert.That(info.AssetName, Is.Null);
            Assert.That(info.DownloadUrl, Is.Null);
        }

        [Test]
        public void Parse_WithHtmlUrlAndBody_SetsFields()
        {
            var json = @"{
                ""html_url"": ""https://github.com/org/repo/releases/tag/v1.0"",
                ""body"": ""## Changelog\n\n- Item 1""
            }";

            var info = ReleaseResponseParser.Parse(json);

            Assert.That(info.ReleaseUrl, Is.EqualTo("https://github.com/org/repo/releases/tag/v1.0"));
            Assert.That(info.Changelog, Does.Contain("Item 1"));
        }
    }
}
