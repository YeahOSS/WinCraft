namespace WinCraft.UI
{
    /// <summary>
    /// Interactive size/move loop state (WM_ENTERSIZEMOVE / WM_EXITSIZEMOVE),
    /// with the kind derived from the preceding SC_MOVE / SC_SIZE command.
    /// </summary>
    public enum SizeMoveState
    {
        /// <summary>No interactive move or resize is in progress.</summary>
        None = 0,
        /// <summary>The window is being dragged (SC_MOVE).</summary>
        Moving,
        /// <summary>A resize edge is being dragged (SC_SIZE).</summary>
        Resizing,
    }
}
