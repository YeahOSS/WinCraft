using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class DropdownHandlerTests
    {
        [Test]
        public void MouseWheel_RaisedOnTheOwnerWhileOpen_IsSwallowed()
        {
            var owner = new Control();
            var isOpen = false;
            var handler = new DropdownHandler(owner, () => isOpen, value => isOpen = value);
            isOpen = true;
            handler.OnOpened();

            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, 120)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
            };
            owner.RaiseEvent(args);

            Assert.That(args.Handled, Is.True);
        }

        [Test]
        public void MouseWheel_RaisedOnTheOwnerAfterClose_IsNotSwallowed()
        {
            var owner = new Control();
            var isOpen = false;
            var handler = new DropdownHandler(owner, () => isOpen, value => isOpen = value);
            isOpen = true;
            handler.OnOpened();
            isOpen = false;
            handler.OnClosed();

            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, 120)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
            };
            owner.RaiseEvent(args);

            Assert.That(args.Handled, Is.False);
        }
    }
}
