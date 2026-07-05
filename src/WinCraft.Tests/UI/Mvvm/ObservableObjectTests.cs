using System.ComponentModel;
using NUnit.Framework;
using WinCraft.UI.Mvvm;

namespace WinCraft.Tests.UI.Mvvm
{
    [TestFixture]
    internal sealed class ObservableObjectTests
    {
        [Test]
        public void GetValue_Default_ReturnsDefault()
        {
            var vm = new TestViewModel();

            Assert.That(vm.Name, Is.Null);
            Assert.That(vm.Count, Is.EqualTo(0));
        }

        [Test]
        public void SetValue_RaisesPropertyChanged()
        {
            var vm = new TestViewModel();
            string changedProperty = null;
            vm.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            vm.Name = "hello";

            Assert.That(changedProperty, Is.EqualTo(nameof(TestViewModel.Name)));
        }

        [Test]
        public void SetValue_SameValue_DoesNotRaisePropertyChanged()
        {
            var vm = new TestViewModel();
            vm.Name = "hello";
            var raised = false;
            vm.PropertyChanged += (s, e) => raised = true;

            vm.Name = "hello";

            Assert.That(raised, Is.False);
        }

        [Test]
        public void SetValue_DifferentValue_RaisesPropertyChanged()
        {
            var vm = new TestViewModel();
            vm.Name = "first";
            string changedProperty = null;
            vm.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            vm.Name = "second";

            Assert.That(changedProperty, Is.EqualTo(nameof(TestViewModel.Name)));
        }

        [Test]
        public void GetValue_ReturnsLastSetValue()
        {
            var vm = new TestViewModel { Name = "stored" };

            Assert.That(vm.Name, Is.EqualTo("stored"));
        }

        [Test]
        public void SetValue_IntType_PreservesValue()
        {
            var vm = new TestViewModel();

            vm.Count = 42;

            Assert.That(vm.Count, Is.EqualTo(42));
        }

        [Test]
        public void RaisePropertyChanged_InvokesEvent()
        {
            var vm = new TestViewModel();
            string changedProperty = null;
            vm.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            vm.TestRaisePropertyChanged("CustomProp");

            Assert.That(changedProperty, Is.EqualTo("CustomProp"));
        }

        private sealed class TestViewModel : ObservableObject
        {
            public string Name
            {
                get => GetValue<string>();
                set => SetValue(value);
            }

            public int Count
            {
                get => GetValue<int>();
                set => SetValue(value);
            }

            public void TestRaisePropertyChanged(string name) => RaisePropertyChanged(name);
        }

        [Test]
        public void IsInDesignMode_DoesNotThrow()
        {
            Assert.That(() => ObservableObject.IsInDesignMode, Throws.Nothing);
        }
    }
}
