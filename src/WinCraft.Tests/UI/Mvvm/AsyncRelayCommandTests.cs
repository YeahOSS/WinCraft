using System;
using System.Threading.Tasks;
using NUnit.Framework;
using WinCraft.UI.Mvvm;

namespace WinCraft.Tests.UI.Mvvm
{
    [TestFixture]
    internal sealed class AsyncRelayCommandTests
    {
        [Test]
        public void Constructor_NullExecute_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new AsyncRelayCommand(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void CanExecute_Idle_ReturnsTrue()
        {
            var cmd = new AsyncRelayCommand(() => Task.FromResult(0));

            Assert.That(cmd.CanExecute(null), Is.True);
        }

        [Test]
        public void CanExecute_WithGuardFalse_ReturnsFalse()
        {
            var cmd = new AsyncRelayCommand(() => Task.FromResult(0), () => false);

            Assert.That(cmd.CanExecute(null), Is.False);
        }

        [Test]
        public void IsExecuting_StartsFalse()
        {
            var cmd = new AsyncRelayCommand(() => Task.FromResult(0));

            Assert.That(cmd.IsExecuting, Is.False);
        }

        [Test]
        public async Task Execute_SetsIsExecutingDuringRun()
        {
            var tcs = new TaskCompletionSource<bool>();
            var cmd = new AsyncRelayCommand(() => tcs.Task);

            // Start execution but don't complete yet
            cmd.Execute(null);
            Assert.That(cmd.IsExecuting, Is.True);

            // Allow completion and wait
            tcs.SetResult(true);
            // Give the continuation a chance to run
            await Task.Delay(50);

            Assert.That(cmd.IsExecuting, Is.False);
        }

        [Test]
        public async Task Execute_ReentrantCall_IsIgnored()
        {
            var callCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var cmd = new AsyncRelayCommand(async () =>
            {
                callCount++;
                await tcs.Task;
            });

            cmd.Execute(null);
            cmd.Execute(null); // Re-entrant — should be ignored

            tcs.SetResult(true);
            await Task.Delay(50);

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Execute_WhenGuardReturnsFalse_DoesNotRun()
        {
            var called = false;
            var cmd = new AsyncRelayCommand(
                () => { called = true; return Task.FromResult(0); }, () => false);

            cmd.Execute(null);
            await Task.Delay(50);

            Assert.That(called, Is.False);
        }
    }

    [TestFixture]
    internal sealed class AsyncRelayCommandGenericTests
    {
        [Test]
        public void Constructor_NullExecute_ThrowsArgumentNullException()
        {
            Assert.That(
                () => new AsyncRelayCommand<string>(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void Execute_WithWrongType_DoesNotThrow()
        {
            var called = false;
            var cmd = new AsyncRelayCommand<string>(_ =>
            {
                called = true;
                return Task.FromResult(0);
            });

            Assert.That(() => cmd.Execute(42), Throws.Nothing);
            Assert.That(called, Is.False);
        }

        [Test]
        public void CanExecute_WithoutGuard_ReturnsTrueEvenForWrongType()
        {
            var cmd = new AsyncRelayCommand<string>(_ => Task.FromResult(0));

            // Without a guard, CanExecute returns true when idle —
            // type filtering only applies when a CanExecuteFunc is set.
            Assert.That(cmd.CanExecute(42), Is.True);
        }

        [Test]
        public void CanExecute_WithGuardAndMatchingType_ReturnsGuardResult()
        {
            var cmd = new AsyncRelayCommand<int>(
                _ => Task.FromResult(0), v => v > 0);

            Assert.That(cmd.CanExecute(5), Is.True);
            Assert.That(cmd.CanExecute(-1), Is.False);
        }
    }
}
