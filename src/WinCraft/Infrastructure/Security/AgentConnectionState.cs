namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Lifecycle states of the elevated agent connection.
    /// Callers can read <see cref="ElevatedAgentController.State"/>
    /// to show a status indicator or guard against duplicate requests
    /// while the agent is starting.
    /// </summary>
    internal enum AgentConnectionState
    {
        /// <summary>The agent is not running and no operation is in progress.</summary>
        Idle,

        /// <summary>The UAC prompt is showing or the agent process is starting.</summary>
        Launching,

        /// <summary>The agent process is running; waiting for the pipe connection.</summary>
        WaitingForConnection,

        /// <summary>The pipe is connected and the agent is ready for requests.</summary>
        Connected,

        /// <summary>The controller has been disposed and is no longer usable.</summary>
        Disposed
    }
}
