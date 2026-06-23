# Elevated Host IPC

WinCraft keeps the visible UI process unelevated at all times.
The UI owns shell integration, drag-and-drop, and single-instance activation.
Privileged work is delegated to an internal host process that can execute
requests as `Administrator` or, when required, through a short-lived
`TrustedInstaller` hop.

## Privilege Model

Every privileged request declares one explicit level:

- `Standard`
- `Administrator`
- `TrustedInstaller`

The UI must not guess, retry, or silently promote a request. Product code picks
the intended level directly at the call site.

## Why the UI stays unelevated

Running the main window elevated breaks normal shell interaction:

- Explorer drag-and-drop and shell messages are blocked by integrity isolation.
- A permanently elevated UI would require a worse startup experience.
- WinCraft only needs higher privileges for a narrow set of infrastructure
  operations, not for the full interactive surface.

The product model is therefore:

- one unelevated UI
- one attached privileged host per UI session
- zero long-lived `TrustedInstaller` processes

## Startup Flows

### Normal launch

1. The user starts WinCraft normally.
2. The unelevated UI starts and owns the named pipe server.
3. The first `Administrator` or `TrustedInstaller` request causes the UI to
   launch the privileged host with `runas`.
4. The host connects back to the UI-owned pipe and stays alive for the rest of
   the UI session.

The user sees UAC once for that session.

### Manual "Run as administrator"

1. The initial elevated process does not create the WPF main window.
2. It becomes the privileged host for that session.
3. The host launches a separate unelevated UI by using the shell token.
4. The unelevated UI starts with `--attach-elevated-agent`, connects to the
   existing host, and becomes the only visible instance.

This keeps drag-and-drop and shell integration available without paying a
second UAC prompt later in the session.

## Process Roles

### Unelevated UI

- owns the named pipe server
- handles single-instance activation
- handles shell interaction and drag-and-drop
- routes privileged work through `PrivilegeBroker`

### Privileged host

- has no main window
- runs either because the UI launched it with `runas` or because the user
  explicitly started WinCraft elevated
- connects as the named pipe client
- executes `Administrator` requests locally
- upgrades individual `TrustedInstaller` requests through the TI hop

### TrustedInstaller hop

The host does not stay in the `TrustedInstaller` context.
For one TI request it does this:

1. duplicate the active-session `winlogon.exe` token and start a temporary
   SYSTEM hop process
2. ensure the `TrustedInstaller` service is running
3. duplicate the `TrustedInstaller.exe` token and start a one-shot TI execute
   process
4. run the requested operation and send the result back through a dedicated
   pipe
5. let the temporary TI process exit immediately after the request

## IPC

The main UI/host channel remains:

- server: unelevated UI
- client: privileged host

This direction avoids the DACL and integrity barriers that make the reverse
direction unreliable.

Messages are still length-prefixed `DataContract` payloads over a named pipe.
Each request now includes:

- operation name
- serialized payload
- request id
- explicit privilege level

Adding a new privileged operation still means:

1. add an operation name in `ElevatedOperations`
2. add the handler in `ElevatedOperationExecutor`
3. choose the required `PrivilegeLevel` at the call site

## Identity Validation

For the long-lived UI/host pipe:

- the UI passes the expected host PID explicitly
- a host launched by the UI receives the expected UI PID explicitly
- a host created by "Run as administrator" binds to the first verified UI pipe
  server that sends a valid request
- the UI validates the connecting client PID on every connection
- the host exits when its bound UI PID disappears

For the TI one-shot pipe:

- the pipe name is random per request
- the request id is carried end-to-end and verified in the result
- the TI execute process connects once, returns one result, and exits

Cross-user scenarios remain out of scope.

## Lifecycle

- the privileged host is bound to one UI session
- the UI sends a shutdown request during normal exit
- if the host is already gone, shutdown is best-effort only
- if the UI PID disappears, the host stops waiting and exits
- TI hop processes are never persistent
