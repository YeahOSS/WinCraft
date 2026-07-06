using System.Threading;
using System.Windows;
using System.Windows.Controls;
using NUnit.Framework;
using WinCraft.Infrastructure.Shell.DragDrop;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class ShellDropTargetTests
    {
        [Test]
        public void Register_SetsAllowDropToTrue()
        {
            var target = new Border { AllowDrop = false };

            ShellDropTarget.Register(target, DragDropEffects.Move);

            Assert.That(target.AllowDrop, Is.True);
        }
    }
}
