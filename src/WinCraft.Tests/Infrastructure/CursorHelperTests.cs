using System;
using System.Drawing;
using System.Threading;
using NUnit.Framework;
using WinCraft.Infrastructure;

namespace WinCraft.Tests.Infrastructure
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class CursorHelperTests
    {
        [Test]
        public void CreateCursorFromBitmap_ValidBitmap_ReturnsCursor()
        {
            using var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Red);
            }

            var cursor = CursorHelper.CreateCursorFromBitmap(bitmap, new Point(0, 0));

            Assert.That(cursor, Is.Not.Null);
        }

        [Test]
        public void CreateCursorFromBitmap_WithCustomHotspot_ReturnsCursor()
        {
            using var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Blue);
            }

            var cursor = CursorHelper.CreateCursorFromBitmap(bitmap, new Point(16, 16));

            Assert.That(cursor, Is.Not.Null);
        }

        [Test]
        public void CreateCursorFromBitmap_SmallBitmap_ReturnsCursor()
        {
            using var bitmap = new Bitmap(1, 1);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Green);
            }

            var cursor = CursorHelper.CreateCursorFromBitmap(bitmap, new Point(0, 0));

            Assert.That(cursor, Is.Not.Null);
        }
    }
}
