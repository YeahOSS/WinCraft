using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    internal sealed class TabCollectionTransferTests
    {
        [Test]
        public void Move_WithinObservableCollection_UsesSingleMoveNotification()
        {
            object first = new();
            object second = new();
            object third = new();
            var tabs = new ObservableCollection<object> { first, second, third };
            NotifyCollectionChangedEventArgs change = null;
            int changeCount = 0;
            tabs.CollectionChanged += (_, e) =>
            {
                change = e;
                changeCount++;
            };

            bool moved = TabCollectionTransfer.Move(tabs, tabs, first, tabs.Count);

            Assert.Multiple(() =>
            {
                Assert.That(moved, Is.True);
                Assert.That(tabs, Is.EqualTo(new[] { second, third, first }));
                Assert.That(changeCount, Is.EqualTo(1));
                Assert.That(change?.Action, Is.EqualTo(NotifyCollectionChangedAction.Move));
            });
        }

        [Test]
        public void Move_BetweenCollections_InsertsAtRequestedBoundary()
        {
            object movedTab = new();
            object existingTab = new();
            IList source = new ObservableCollection<object> { movedTab };
            IList target = new ObservableCollection<object> { existingTab };

            bool moved = TabCollectionTransfer.Move(source, target, movedTab, 0);

            Assert.Multiple(() =>
            {
                Assert.That(moved, Is.True);
                Assert.That(source, Is.Empty);
                Assert.That(target, Is.EqualTo(new[] { movedTab, existingTab }));
            });
        }

        [Test]
        public void Move_SameBoundary_DoesNotMutateCollection()
        {
            object first = new();
            object second = new();
            var tabs = new ObservableCollection<object> { first, second };
            int changeCount = 0;
            tabs.CollectionChanged += (_, _) => changeCount++;

            bool moved = TabCollectionTransfer.Move(tabs, tabs, first, 1);

            Assert.Multiple(() =>
            {
                Assert.That(moved, Is.False);
                Assert.That(tabs, Is.EqualTo(new[] { first, second }));
                Assert.That(changeCount, Is.Zero);
            });
        }

        [Test]
        public void MoveToIndex_UsesFinalVisualIndex()
        {
            object first = new();
            object second = new();
            object third = new();
            var tabs = new ObservableCollection<object> { first, second, third };

            bool moved = TabCollectionTransfer.MoveToIndex(tabs, first, 2);

            Assert.Multiple(() =>
            {
                Assert.That(moved, Is.True);
                Assert.That(tabs, Is.EqualTo(new[] { second, third, first }));
            });
        }

        [TestCase(0, 0, 2, 2)]
        [TestCase(1, 0, 2, 0)]
        [TestCase(2, 0, 2, 1)]
        [TestCase(0, 2, 0, 1)]
        [TestCase(1, 2, 0, 2)]
        [TestCase(2, 2, 0, 0)]
        public void WindowTabPanel_GetVisualIndex_MovesOnlyAffectedNeighbors(
            int itemIndex,
            int draggedIndex,
            int previewIndex,
            int expectedVisualIndex)
        {
            Assert.That(
                WindowTabPanel.GetVisualIndex(itemIndex, draggedIndex, previewIndex),
                Is.EqualTo(expectedVisualIndex));
        }

        [Test]
        public void Move_FixedSizeTarget_IsRejected()
        {
            object tab = new();
            IList source = new ObservableCollection<object> { tab };
            IList target = new object[1];

            bool moved = TabCollectionTransfer.Move(source, target, tab, 0);

            Assert.Multiple(() =>
            {
                Assert.That(moved, Is.False);
                Assert.That(source, Is.EqualTo(new[] { tab }));
            });
        }
    }
}
