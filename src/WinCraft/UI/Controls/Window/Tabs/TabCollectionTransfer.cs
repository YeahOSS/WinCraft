using System;
using System.Collections;
using WinCraft.Infrastructure;

namespace WinCraft.UI
{
    internal static class TabCollectionTransfer
    {
        public static bool Move(IList source, IList target, object item, int insertionIndex)
        {
            if (source == null || target == null || item == null)
                return false;

            int sourceIndex = IndexOfReference(source, item);
            if (sourceIndex < 0)
                return false;

            if (ReferenceEquals(source, target))
                return MoveWithin(source, sourceIndex, insertionIndex);

            if (!CanModify(source)
                || !CanModify(target)
                || IndexOfReference(target, item) >= 0)
            {
                return false;
            }

            int targetIndex = Math.Max(0, Math.Min(insertionIndex, target.Count));
            source.RemoveAt(sourceIndex);
            try
            {
                target.Insert(targetIndex, item);
            }
            catch
            {
                source.Insert(Math.Min(sourceIndex, source.Count), item);
                throw;
            }
            return true;
        }

        public static bool MoveToIndex(IList list, object item, int targetIndex)
        {
            if (list == null || item == null || !CanModify(list))
                return false;

            int sourceIndex = IndexOfReference(list, item);
            if (sourceIndex < 0)
                return false;

            targetIndex = Math.Max(0, Math.Min(targetIndex, list.Count - 1));
            if (sourceIndex == targetIndex)
                return false;

            return MoveWithinIndices(list, sourceIndex, targetIndex);
        }

        public static bool CanModify(IList list) =>
            list != null && !list.IsFixedSize && !list.IsReadOnly;

        public static int IndexOfReference(IList list, object item)
        {
            if (list == null)
                return -1;

            for (int index = 0; index < list.Count; index++)
            {
                if (ReferenceEquals(list[index], item))
                    return index;
            }

            return -1;
        }

        private static bool MoveWithin(IList list, int sourceIndex, int insertionIndex)
        {
            if (!CanModify(list))
                return false;

            int targetIndex = Math.Max(0, Math.Min(insertionIndex, list.Count));
            if (targetIndex > sourceIndex)
                targetIndex--;

            if (targetIndex == sourceIndex)
                return false;

            return MoveWithinIndices(list, sourceIndex, targetIndex);
        }

        private static bool MoveWithinIndices(IList list, int sourceIndex, int targetIndex)
        {
            if (list.TryInvokeInstanceMethod("Move", sourceIndex, targetIndex))
                return true;

            object item = list[sourceIndex];
            list.RemoveAt(sourceIndex);
            list.Insert(targetIndex, item);
            return true;
        }
    }
}
