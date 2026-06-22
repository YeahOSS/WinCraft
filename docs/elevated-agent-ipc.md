# Elevated Agent IPC

The UI process launches a persistent elevated agent once per session via UAC.
The agent stays alive and handles privileged operations on demand, so the user
sees the UAC prompt only once.

## Why not just run as administrator?

Running the entire application elevated is simpler but has several downsides:

- **UAC on every launch.** An admin-only app would prompt UAC each time it
  starts, including auto-start on login — a poor user experience.
- **Integrity-level isolation.** Elevated processes cannot receive drag-and-drop
  or shell messages from non-elevated programs. File dialogs and Explorer
  integration behave differently.
- **Least privilege.** The UI and most features run with standard rights. Only
  specific operations (HKLM writes, privileged process execution) need
  elevation.

The dual-process model keeps the UI unelevated and delegates only the
operations that truly require administrator rights.

## Architecture

The UI owns the named pipe server; the elevated agent connects as a client.
This direction avoids the integrity-level and DACL barriers that block the
reverse.

## Lifecycle

1. On first privileged request, the UI creates a named pipe and launches
   itself with `--elevated-agent --pipe-name <name>` via the `runas` verb.
2. The agent connects, the server verifies the client PID, and the
   request/response cycle begins.
3. After each operation the server disconnects the pipe so the agent can
   reconnect for the next request — no need to relaunch.
4. On shutdown the UI sends a shutdown request. If the agent does not exit
   promptly the controller kills the process.

## Protocol

Length-prefixed DataContract messages over a named pipe. Operations are
identified by string names defined in `ElevatedOperations`. Adding a new
privileged operation means adding a new operation name and a handler in
`ElevatedOperationExecutor`.

## Security

The pipe name embeds the UI process ID. `GetNamedPipeClientProcessId` is
called after every connection — connections from unexpected processes are
rejected. Cross-user scenarios are out of scope.
