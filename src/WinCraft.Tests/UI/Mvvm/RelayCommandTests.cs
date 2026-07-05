using System;
using NUnit.Framework;
using WinCraft.UI.Mvvm;

namespace WinCraft.Tests.UI.Mvvm
{
    [TestFixture]
    internal sealed class RelayCommandTests
    {
        [Test]
        public void Constructor_NullAction_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new RelayCommand(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void Execute_InvokesAction()
        {
            var called = false;
            var cmd = new RelayCommand(() => { called = true; });

            cmd.Execute(null);

            Assert.That(called, Is.True);
        }

        [Test]
        public void CanExecute_NoGuard_ReturnsTrue()
        {
            var cmd = new RelayCommand(() => { });

            Assert.That(cmd.CanExecute(null), Is.True);
        }

        [Test]
        public void CanExecute_WithGuardFalse_ReturnsFalse()
        {
            var cmd = new RelayCommand(() => { }) { CanExecuteFunc = () => false };

            Assert.That(cmd.CanExecute(null), Is.False);
        }

        [Test]
        public void CanExecute_WithGuardTrue_ReturnsTrue()
        {
            var cmd = new RelayCommand(() => { }) { CanExecuteFunc = () => true };

            Assert.That(cmd.CanExecute(null), Is.True);
        }

        [Test]
        public void CanExecuteChanged_CanBeSubscribed()
        {
            var cmd = new RelayCommand(() => { });
            var raised = false;
            EventHandler handler = (s, e) => raised = true;

            cmd.CanExecuteChanged += handler;
            // CommandManager.RequerySuggested fires asynchronously;
            // just verify subscription works without exception.
            cmd.CanExecuteChanged -= handler;

            Assert.That(raised, Is.False);
        }
    }

    [TestFixture]
    internal sealed class RelayCommandGenericTests
    {
        [Test]
        public void Execute_WithMatchingType_InvokesAction()
        {
            string received = null;
            var cmd = new RelayCommand<string>(value => { received = value; });

            cmd.Execute("hello");

            Assert.That(received, Is.EqualTo("hello"));
        }

        [Test]
        public void Execute_WithWrongType_DoesNotThrow()
        {
            var called = false;
            var cmd = new RelayCommand<string>(_ => { called = true; });

            Assert.That(() => cmd.Execute(42), Throws.Nothing);
            Assert.That(called, Is.False);
        }

        [Test]
        public void CanExecute_WithoutGuard_ReturnsTrueEvenForWrongType()
        {
            var cmd = new RelayCommand<string>(_ => { });

            // Without a guard, CanExecute always returns true —
            // type filtering only applies when a CanExecuteFunc is set.
            Assert.That(cmd.CanExecute(42), Is.True);
        }

        [Test]
        public void CanExecute_WithNullForReferenceType_ReturnsTrue()
        {
            var cmd = new RelayCommand<string>(_ => { });

            Assert.That(cmd.CanExecute(null), Is.True);
        }

        [Test]
        public void CanExecute_WithGuardAndMatchingType_ReturnsGuardResult()
        {
            var cmd = new RelayCommand<int>(_ => { }) { CanExecuteFunc = v => v > 0 };

            Assert.That(cmd.CanExecute(5), Is.True);
            Assert.That(cmd.CanExecute(0), Is.False);
        }
    }
}
