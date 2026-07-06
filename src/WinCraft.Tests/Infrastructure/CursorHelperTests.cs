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

    }
}
