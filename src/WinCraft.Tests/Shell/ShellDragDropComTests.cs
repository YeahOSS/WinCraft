using System;
using System.Drawing;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows;
using NUnit.Framework;
using WinCraft.Infrastructure.Shell;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using Point = System.Drawing.Point;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class ShellDragDropComTests
    {
        private Window _window;
        private IntPtr _hwnd;

        [SetUp]
        public void SetUp()
        {
            _window = new Window
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                AllowsTransparency = true,
                Background = null,
            };
            _hwnd = new System.Windows.Interop.WindowInteropHelper(_window).EnsureHandle();
        }

        [TearDown]
        public void TearDown()
        {
            _window?.Close();
            _window = null;
        }

        [Test]
        public void CDragDropHelper_CanCreate_AndCastToBothInterfaces()
        {
            var helper = new CDragDropHelper();

            var dropHelper = (IDropTargetHelper)helper;
            Assert.That(dropHelper, Is.Not.Null);

            var sourceHelper = (IDragSourceHelper)helper;
            Assert.That(sourceHelper, Is.Not.Null);

            var sourceHelper2 = (IDragSourceHelper2)helper;
            Assert.That(sourceHelper2, Is.Not.Null);
        }

        [Test]
        public void DragSourceHelper_InitializeFromBitmap_ReturnsOK()
        {
            using var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
                g.Clear(Color.Orange);

            using var data = new ShellDataObject();
            var image = new SHDRAGIMAGE
            {
                hbmpDragImage = (HBITMAP)bitmap.GetHbitmap(Color.FromArgb(0)),
                sizeDragImage = bitmap.Size,
                ptOffset = new Point(0, bitmap.Height),
                crColorKey = (COLORREF)uint.MaxValue,
            };

            var helper = (IDragSourceHelper)new CDragDropHelper();
            int hr = helper.InitializeFromBitmap(ref image, data);

            Assert.That(hr, Is.EqualTo(0)); // S_OK
        }

        [Test]
        public void DragSourceHelper2_SetFlags_ReturnsOK()
        {
            var helper = (IDragSourceHelper2)new CDragDropHelper();

            int hr = helper.SetFlags((int)DSH_FLAGS.DSH_ALLOWDROPDESCRIPTIONTEXT);

            Assert.That(hr, Is.EqualTo(0)); // S_OK
        }

        [Test]
        public void DropTargetHelper_DragEnterDragLeave_ReturnsOK()
        {
            using var data = new ShellDataObject();
            data.SetText("drag-test");
            var pt = new Point(10, 10);

            var helper = (IDropTargetHelper)new CDragDropHelper();

            int enterHr = helper.DragEnter(_hwnd, data, ref pt, (int)DragDropEffects.Copy);
            Assert.That(enterHr, Is.EqualTo(0)); // S_OK

            int leaveHr = helper.DragLeave();
            Assert.That(leaveHr, Is.EqualTo(0)); // S_OK
        }

        [Test]
        public void DropTargetHelper_DragOver_AfterDragEnter_ReturnsOK()
        {
            using var data = new ShellDataObject();
            data.SetText("drag-over-test");
            var pt = new Point(5, 5);

            var helper = (IDropTargetHelper)new CDragDropHelper();
            helper.DragEnter(_hwnd, data, ref pt, (int)DragDropEffects.Copy);

            pt.X = 15;
            pt.Y = 15;
            int hr = helper.DragOver(ref pt, (int)DragDropEffects.Copy);

            Assert.That(hr, Is.EqualTo(0)); // S_OK
            helper.DragLeave();
        }

        [Test]
        public void DropTargetHelper_Drop_AfterDragEnter_ReturnsOK()
        {
            using var data = new ShellDataObject();
            data.SetText("drop-test");
            var pt = new Point(20, 20);

            var helper = (IDropTargetHelper)new CDragDropHelper();
            helper.DragEnter(_hwnd, data, ref pt, (int)DragDropEffects.Copy);

            int hr = helper.Drop(data, ref pt, (int)DragDropEffects.Copy);

            Assert.That(hr, Is.EqualTo(0)); // S_OK
        }

        [Test]
        public void DropTargetHelper_Show_ReturnsOK()
        {
            var helper = (IDropTargetHelper)new CDragDropHelper();

            int hideHr = helper.Show(false);
            Assert.That(hideHr, Is.EqualTo(0)); // S_OK

            int showHr = helper.Show(true);
            Assert.That(showHr, Is.EqualTo(0)); // S_OK
        }
    }
}
