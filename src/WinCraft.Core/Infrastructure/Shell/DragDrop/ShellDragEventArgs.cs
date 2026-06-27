using System.Runtime.InteropServices.ComTypes;
using Windows.Win32;
using DrawingPoint = System.Drawing.Point;

namespace WinCraft.Infrastructure.Shell.DragDrop
{
    /// <summary>
    /// Wraps WPF drag event data for Shell drag helpers.
    /// </summary>
    public struct ShellDragEventArgs
    {
        public IDataObject Data { get; private set; }
        public DrawingPoint Point { get; private set; }
        public System.Windows.DragDropEffects Effects { get; private set; }
        public int KeyStates { get; private set; }

        public ShellDragEventArgs(System.Windows.DragEventArgs e)
        {
            Data = (IDataObject)e.Data;
            PInvoke.GetCursorPos(out DrawingPoint cursorPos);
            Point = cursorPos;
            Effects = e.Effects;
            KeyStates = (int)e.KeyStates;
        }
    }
}
