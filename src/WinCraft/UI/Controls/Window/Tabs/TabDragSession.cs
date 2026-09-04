using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace WinCraft.UI
{
    internal sealed class TabDragSession
    {
        private const double DetachMagnetism = 18;
        private const double AttachPreviewLockDistance = 24;

        private readonly WindowTabStrip _captureStrip;
        private readonly TabWindow _sourceWindow;
        private readonly object _tab;
        private readonly int _sourceIndex;
        private readonly object _sourceSelection;
        private readonly double _grabRatio;
        private readonly double _grabY;
        private readonly WindowState _sourceWindowState;
        private readonly Rect _sourceBounds;
        private readonly List<TabWindow> _visitedWindows = [];

        private TabWindow _currentWindow;
        private WindowTabStrip _currentStrip;
        private TabWindow _floatingWindow;
        private Point _lastScreenPoint;
        private double _attachLockScreenX;
        private int _attachLockIndex = -1;
        private bool _hasScreenPoint;
        private bool _isFloating;
        private bool _isWholeWindowDrag;
        private bool _isSourceWindowConcealed;
        private bool _isFinished;

        public TabDragSession(
            WindowTabStrip captureStrip,
            TabWindow sourceWindow,
            object tab,
            WindowTabItem sourceItem,
            object sourceSelection,
            double grabRatio,
            double grabY)
        {
            _captureStrip = captureStrip;
            _sourceWindow = sourceWindow;
            _currentWindow = sourceWindow;
            _currentStrip = captureStrip;
            _tab = tab;
            _sourceIndex = TabCollectionTransfer.IndexOfReference(sourceWindow.Tabs, tab);
            _sourceSelection = sourceSelection;
            _grabRatio = grabRatio;
            _grabY = grabY;
            _sourceWindowState = sourceWindow.WindowState;
            _sourceBounds = sourceWindow.WindowState == WindowState.Normal
                ? new Rect(sourceWindow.Left, sourceWindow.Top, sourceWindow.ActualWidth, sourceWindow.ActualHeight)
                : sourceWindow.RestoreBounds;

            AddVisitedWindow(sourceWindow);
            Point startPoint = sourceItem.PointToScreen(
                new Point(sourceItem.ActualWidth * grabRatio, grabY));
            _lastScreenPoint = startPoint;
            _hasScreenPoint = true;
            captureStrip.BeginPreview(tab, startPoint, grabRatio);
            InputManager.Current.PreProcessInput += OnPreProcessInput;
        }

        public void Update(Point screenPoint)
        {
            if (_isFinished)
                return;

            if (_isFloating)
            {
                MoveFloatingWindow(screenPoint);
                WindowTabStrip floatingTarget = WindowTabStrip.FindTarget(
                    screenPoint,
                    _tab,
                    _floatingWindow);
                if (floatingTarget != null)
                    AttachTo(floatingTarget, screenPoint);

                _lastScreenPoint = screenPoint;
                _hasScreenPoint = true;
                return;
            }

            WindowTabStrip target = WindowTabStrip.FindTarget(screenPoint, _tab, null);
            if (target != null && !ReferenceEquals(target, _currentStrip))
            {
                AttachTo(target, screenPoint);
                return;
            }

            if (ReferenceEquals(target, _currentStrip)
                || _currentStrip.IsWithinVerticalBand(screenPoint, DetachMagnetism))
            {
                UpdateAttachedPreview(screenPoint);
                _lastScreenPoint = screenPoint;
                _hasScreenPoint = true;
                return;
            }

            if (_currentWindow.CanTearOffTab(_tab))
                Detach(screenPoint);
        }

        public void Complete()
        {
            if (_isFinished)
                return;

            try
            {
                if (!_isFloating && _currentStrip != null)
                {
                    int targetIndex = _currentStrip.GetPreviewIndex();
                    if (targetIndex >= 0)
                        _currentWindow.MoveTabForDrag(_tab, targetIndex);

                    _currentStrip.EndPreview();
                }

                _currentWindow.SelectTabForDrag(_tab);
                _currentWindow.CompleteTabDrag();
                CloseEmptyVisitedWindows(_currentWindow);
                CloseUnusedFloatingWindow();
            }
            finally
            {
                Finish();
            }
        }

        public void Cancel()
        {
            if (_isFinished)
                return;

            try
            {
                _currentStrip?.EndPreview();

                if (!_sourceWindow.ContainsTab(_tab)
                    && _currentWindow.ContainsTab(_tab))
                {
                    _currentWindow.TransferTabForDrag(
                        _sourceWindow,
                        _tab,
                        Math.Max(0, Math.Min(_sourceIndex, _sourceWindow.Tabs.Count)));
                }

                if (_sourceWindow.ContainsTab(_tab))
                    _sourceWindow.MoveTabForDrag(_tab, _sourceIndex);

                RestoreSourceWindowBounds();
                _sourceWindow.SelectTabForDrag(
                    _sourceWindow.ContainsTab(_sourceSelection)
                        ? _sourceSelection
                        : _tab);
                _sourceWindow.CompleteTabDrag();
                CloseEmptyVisitedWindows(_sourceWindow);
                CloseUnusedFloatingWindow();
            }
            finally
            {
                Finish();
            }
        }

        private void Detach(Point screenPoint)
        {
            _currentStrip.EndPreview();
            _attachLockIndex = -1;

            if (_currentWindow.Tabs.Count == 1
                && ReferenceEquals(_currentWindow, _sourceWindow)
                && _floatingWindow == null)
            {
                _isWholeWindowDrag = true;
                _floatingWindow = _sourceWindow;
                _currentWindow.WindowState = WindowState.Normal;
                _currentWindow.UpdateLayout();
            }
            else
            {
                if (_floatingWindow == null)
                    _floatingWindow = _currentWindow.CreateDragSiblingWindow();

                TabWindow previousWindow = _currentWindow;
                if (_floatingWindow == null
                    || !previousWindow.TransferTabForDrag(
                        _floatingWindow,
                        _tab,
                        _floatingWindow.NormalizeInsertionIndex(0)))
                {
                    _currentStrip.BeginPreview(_tab, screenPoint, _grabRatio);
                    return;
                }

                RestoreSourceSelectionAfterTransfer(previousWindow);

                AddVisitedWindow(_floatingWindow);
                _currentWindow = _floatingWindow;
                _currentWindow.ShowForTabDrag();
                _currentWindow.SetTabDragVisual(_tab, true);
            }

            _currentStrip = null;
            _isFloating = true;
            _currentWindow.AlignTabToScreenPoint(
                _tab,
                _grabRatio,
                _grabY,
                screenPoint);
            if (ReferenceEquals(_currentWindow, _sourceWindow))
                RevealSourceWindow();
            _lastScreenPoint = screenPoint;
            _hasScreenPoint = true;
        }

        private void MoveFloatingWindow(Point screenPoint)
        {
            if (!_hasScreenPoint)
                return;

            _currentWindow.MoveByScreenPixels(
                screenPoint.X - _lastScreenPoint.X,
                screenPoint.Y - _lastScreenPoint.Y);
        }

        private void AttachTo(WindowTabStrip targetStrip, Point screenPoint)
        {
            var targetWindow = targetStrip.GetOwner();
            if (targetWindow == null
                || ReferenceEquals(targetStrip, _currentStrip))
            {
                return;
            }

            int insertionIndex = targetStrip.GetExternalInsertionIndex(screenPoint);
            TabWindow previousWindow = _currentWindow;
            _currentStrip?.EndPreview();

            if (!previousWindow.TransferTabForDrag(
                    targetWindow,
                    _tab,
                    insertionIndex))
            {
                _currentStrip?.BeginPreview(
                    _tab,
                    screenPoint,
                    _grabRatio);
                return;
            }

            RestoreSourceSelectionAfterTransfer(previousWindow);

            AddVisitedWindow(targetWindow);
            if (ReferenceEquals(previousWindow, _floatingWindow))
            {
                if (ReferenceEquals(_floatingWindow, _sourceWindow))
                    ConcealSourceWindow();
                else
                    _floatingWindow.Hide();
            }

            _currentWindow = targetWindow;
            _currentStrip = targetStrip;
            _isFloating = false;
            targetWindow.UpdateLayout();
            _attachLockIndex = Math.Max(
                0,
                Math.Min(insertionIndex, targetWindow.Tabs.Count - 1));
            _attachLockScreenX = screenPoint.X;
            targetStrip.BeginPreview(
                _tab,
                screenPoint,
                _grabRatio,
                _attachLockIndex);
            targetWindow.BringToFrontForTabDrag();
            _lastScreenPoint = screenPoint;
            _hasScreenPoint = true;
        }

        private void RestoreSourceSelectionAfterTransfer(TabWindow previousWindow)
        {
            if (ReferenceEquals(previousWindow, _sourceWindow))
                _sourceWindow.RestoreSelectionAfterTabDrag(_sourceSelection);
        }

        private void UpdateAttachedPreview(Point screenPoint)
        {
            int? forcedPreviewIndex = null;
            if (_attachLockIndex >= 0)
            {
                if (Math.Abs(screenPoint.X - _attachLockScreenX)
                    <= AttachPreviewLockDistance)
                {
                    forcedPreviewIndex = _attachLockIndex;
                }
                else
                {
                    _attachLockIndex = -1;
                }
            }

            _currentStrip.UpdatePreview(
                screenPoint,
                _grabRatio,
                forcedPreviewIndex);
        }

        private void RestoreSourceWindowBounds()
        {
            if (!_isWholeWindowDrag || _sourceBounds.IsEmpty)
                return;

            _sourceWindow.WindowState = WindowState.Normal;
            _sourceWindow.Left = _sourceBounds.Left;
            _sourceWindow.Top = _sourceBounds.Top;
            _sourceWindow.Width = _sourceBounds.Width;
            _sourceWindow.Height = _sourceBounds.Height;
            if (_sourceWindowState != WindowState.Normal)
                _sourceWindow.WindowState = _sourceWindowState;
        }

        private void AddVisitedWindow(TabWindow window)
        {
            if (window != null && !_visitedWindows.Contains(window))
                _visitedWindows.Add(window);
        }

        private void ConcealSourceWindow()
        {
            if (_isSourceWindowConcealed)
                return;

            _sourceWindow.ParkOutsideWorkspaceForTabDrag();
            _isSourceWindowConcealed = true;
        }

        private void RevealSourceWindow()
        {
            if (!_isSourceWindowConcealed)
                return;

            _isSourceWindowConcealed = false;
        }

        private void RestoreConcealedSourceAfterFinish()
        {
            if (!_isSourceWindowConcealed)
                return;

            if (_sourceWindow.ContainsTab(_tab))
            {
                RestoreSourceWindowBounds();
                RevealSourceWindow();
                return;
            }

            _sourceWindow.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    if (_sourceWindow.IsVisible)
                    {
                        RestoreSourceWindowBounds();
                        RevealSourceWindow();
                    }
                }));
        }

        private void CloseEmptyVisitedWindows(TabWindow replacement)
        {
            foreach (TabWindow window in _visitedWindows.Where(
                window => !ReferenceEquals(window, replacement)))
            {
                window.CloseIfEmptyAfterDrag(replacement);
            }
        }

        private void CloseUnusedFloatingWindow()
        {
            if (_floatingWindow == null
                || ReferenceEquals(_floatingWindow, _sourceWindow)
                || ReferenceEquals(_floatingWindow, _currentWindow)
                || _floatingWindow.Tabs.Count != 0)
            {
                return;
            }

            TabWindow window = _floatingWindow;
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    if (window.Tabs.Count == 0)
                        window.Close();
                }));
        }

        private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is KeyEventArgs keyEvent
                && keyEvent.RoutedEvent == Keyboard.KeyDownEvent
                && keyEvent.Key == Key.Escape)
            {
                _captureStrip.CancelActiveDrag();
                keyEvent.Handled = true;
            }
        }

        private void Finish()
        {
            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            _sourceWindow.SetTabDragVisual(_tab, false);
            if (!ReferenceEquals(_currentWindow, _sourceWindow))
                _currentWindow.SetTabDragVisual(_tab, false);
            RestoreConcealedSourceAfterFinish();
            _isFinished = true;
        }
    }
}
