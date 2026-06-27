using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using NUnit.Framework;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.Shell;
using WinCraft.Infrastructure.Shell.DragDrop;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class ShellDropTargetTests
    {
        private Window _window;

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
            // Create the HWND without showing the window.
            new System.Windows.Interop.WindowInteropHelper(_window).EnsureHandle();
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
        }

        [Test]
        public void Register_ValidElement_DoesNotThrow()
        {
            var target = new Border();

            Assert.That(() => ShellDropTarget.Register(
                target, DragDropEffects.Copy), Throws.Nothing);
        }

        [Test]
        public void Register_SetsAllowDropToTrue()
        {
            var target = new Border { AllowDrop = false };

            ShellDropTarget.Register(target, DragDropEffects.Move);

            Assert.That(target.AllowDrop, Is.True);
        }

        [Test]
        public void Register_WithNullEffect_DoesNotThrow()
        {
            var target = new Border();

            Assert.That(() => ShellDropTarget.Register(
                target, DragDropEffects.None), Throws.Nothing);
        }

        [Test]
        public void ClearDropDescription_DoesNotThrow()
        {
            Assert.That(() => ShellDropTarget.ClearDropDescription(), Throws.Nothing);
        }
    }
}
