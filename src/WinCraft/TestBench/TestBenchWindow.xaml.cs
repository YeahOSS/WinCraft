using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Security;
using WinCraft.Infrastructure.Shell;
using WinCraft.Infrastructure.Shell.DragDrop;

namespace WinCraft.TestBench
{
    public partial class TestBenchWindow : Window
    {
        private static readonly Task Done = Task.FromResult<object>(null);

        public TestBenchWindow()
        {
            InitializeComponent();

            ShellDropTarget.Register(DropTarget, System.Windows.DragDropEffects.Copy, OnDrop);
        }

        private void Append(string text)
        {
            Dispatcher.Invoke(() =>
            {
                OutputBox.AppendText(text + Environment.NewLine);
                OutputBox.ScrollToEnd();
            });
        }

        private async Task RunTest(string name, Func<Task> test)
        {
            Append($"[{DateTime.Now:HH:mm:ss}] {name}");
            try
            {
                await Task.Run(test);
            }
            catch (Exception ex)
            {
                Append($"  FAILED: {ex.Message}");
            }
        }

        private async void OnCurrentElevation(object sender, RoutedEventArgs e)
        {
            await RunTest("Current Elevation Check", () =>
            {
                var isElevated = ProcessElevation.IsCurrentProcessElevated();
                Append($"  IsCurrentProcessElevated: {isElevated}");

                return Done;
            });
        }

        private async void OnTokenInfo(object sender, RoutedEventArgs e)
        {
            await RunTest("Token / Session Info", () =>
            {
                try
                {
                    var sessionId = Process.GetCurrentProcess().SessionId;
                    Append($"  Session ID: {sessionId}");

                    using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                    Append($"  User: {identity.Name}");
                    Append($"  Authentication: {identity.AuthenticationType}");
                    Append($"  IsAuthenticated: {identity.IsAuthenticated}");
                }
                catch (Exception ex)
                {
                    Append($"  Error: {ex.Message}");
                }

                return Done;
            });
        }

        private async void OnPipeServerWait(object sender, RoutedEventArgs e)
        {
            await RunTest("Pipe Server Wait", () =>
            {
                var pipeName = $"WinCraft.TestBench.{Guid.NewGuid():N}";
                Append($"  Pipe name: {pipeName}");

                using var pipeHandle = ElevatedAgentPipeServer.Create(pipeName);
                Append("  Server pipe created.");

                try
                {
                    ElevatedAgentPipeServer.WaitForConnection(pipeHandle);
                    Append("  Client connected (wait completed).");
                }
                catch (TimeoutException)
                {
                    Append("  No client connected within timeout (expected).");
                }

                return Done;
            });
        }

        private async void OnAgentLifecycle(object sender, RoutedEventArgs e)
        {
            await RunTest("Agent Lifecycle", () =>
            {
                using var controller = new ElevatedAgentController();
                Append("  Controller created.");

                try
                {
                    var pingRequest = new ElevatedCommandRequest
                    {
                        OperationName = ElevatedOperations.Ping,
                        PrivilegeLevel = PrivilegeLevel.Administrator,
                        RequestId = Guid.NewGuid().ToString("N")
                    };

                    Append("  Sending Ping...");
                    var result = controller.Execute(pingRequest);

                    if (result != null && result.Succeeded)
                    {
                        Append($"  Ping succeeded (id={result.RequestId}).");

                        var shutdownRequest = new ElevatedCommandRequest
                        {
                            OperationName = ElevatedOperations.Shutdown,
                            PrivilegeLevel = PrivilegeLevel.Administrator,
                            RequestId = Guid.NewGuid().ToString("N")
                        };

                        Append("  Sending Shutdown...");
                        controller.Execute(shutdownRequest);
                        Append("  Shutdown sent.");
                    }
                    else
                    {
                        Append($"  Ping failed: {result?.ErrorCode} - {result?.ErrorMessage}");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Append($"  Agent error: {ex.Message}");
                }

                return Done;
            });
        }

        private async void OnRegistryWrite(object sender, RoutedEventArgs e)
        {
            await RunTest("Registry Write HKLM (Admin)", () =>
            {
                var writer = new PrivilegedRegistryWriter(ApplicationServices.PrivilegeBroker);

                var result = writer.WriteString(
                    RegistryValueLocation.LocalMachine,
                    @"SOFTWARE\WinCraft\Test",
                    "TestValue",
                    DateTime.Now.ToString("O"),
                    PrivilegeLevel.Administrator);

                Append(result.Succeeded
                    ? "  Write succeeded."
                    : $"  Write failed: {result.ErrorCode} - {result.ErrorMessage}");

                return Done;
            });
        }

        private async void OnRegistryDelete(object sender, RoutedEventArgs e)
        {
            await RunTest("Registry Delete HKLM (Admin)", () =>
            {
                var writer = new PrivilegedRegistryWriter(ApplicationServices.PrivilegeBroker);

                var result = writer.DeleteString(
                    RegistryValueLocation.LocalMachine,
                    @"SOFTWARE\WinCraft\Test",
                    "TestValue",
                    PrivilegeLevel.Administrator);

                Append(result.Succeeded
                    ? "  Delete succeeded."
                    : $"  Delete failed: {result.ErrorCode} - {result.ErrorMessage}");

                return Done;
            });
        }

        private async void OnTiServiceStatus(object sender, RoutedEventArgs e)
        {
            await RunTest("TI Service Status", () =>
            {
                try
                {
                    using var sc = new System.ServiceProcess.ServiceController("TrustedInstaller");
                    Append($"  Status: {sc.Status}");
                    Append($"  ServiceType: {sc.ServiceType}");
                }
                catch (InvalidOperationException ex)
                {
                    Append($"  Error: {ex.Message}");
                }

                return Done;
            });
        }

        private async void OnTiAccessValidation(object sender, RoutedEventArgs e)
        {
            await RunTest("TI Access Validation", () =>
            {
                const string tiProtectedPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing";
                const string testValueName = "WinCraft.TestBench.Probe";
                var writer = new PrivilegedRegistryWriter(ApplicationServices.PrivilegeBroker);

                Append($"  Target: HKLM\\{tiProtectedPath}");
                Append($"  Value: {testValueName}");

                // Step 1: attempt as Administrator — must fail on this TI-owned key.
                Append("  --- Admin attempt (should fail) ---");
                var adminResult = writer.WriteString(
                    RegistryValueLocation.LocalMachine,
                    tiProtectedPath,
                    testValueName,
                    "admin-probe",
                    PrivilegeLevel.Administrator);

                Append(adminResult.Succeeded
                    ? "  Admin write SUCCEEDED — key is NOT TI-protected, test invalid."
                    : $"  Admin write FAILED (expected): {adminResult.ErrorCode}");

                // Step 2: attempt as TrustedInstaller — must succeed.
                Append("  --- TI attempt (should succeed) ---");
                var tiResult = writer.WriteString(
                    RegistryValueLocation.LocalMachine,
                    tiProtectedPath,
                    testValueName,
                    DateTime.Now.ToString("O"),
                    PrivilegeLevel.TrustedInstaller);

                if (!tiResult.Succeeded)
                {
                    Append($"  TI write FAILED: {tiResult.ErrorCode} - {tiResult.ErrorMessage}");
                    return Done;
                }

                Append("  TI write succeeded.");

                // Step 3: clean up via TI.
                Append("  --- Cleanup ---");
                var deleteResult = writer.DeleteString(
                    RegistryValueLocation.LocalMachine,
                    tiProtectedPath,
                    testValueName,
                    PrivilegeLevel.TrustedInstaller);

                Append(deleteResult.Succeeded
                    ? "  Cleanup succeeded."
                    : $"  Cleanup failed: {deleteResult.ErrorCode} - {deleteResult.ErrorMessage}");

                return Done;
            });
        }

        private void OnDragSourceMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var data = new ShellDataObject();
            data.SetText("Hello from WinCraft TestBench!");

            using (var bitmap = CreateDragBitmap())
            {
                ShellDragSource.DoDragDrop(
                    DragSource,
                    data,
                    System.Windows.DragDropEffects.Copy | System.Windows.DragDropEffects.Move,
                    bitmap);
            }

            data.Dispose();
        }

        private void OnDrop(ShellDragEventArgs args)
        {
            Append($"  Drop received — Effects: {args.Effects}");

            var textStream = args.Data.GetStream(System.Windows.DataFormats.UnicodeText);
            if (textStream != null)
            {
                using (textStream)
                using (var reader = new System.IO.StreamReader(textStream, System.Text.Encoding.Unicode))
                {
                    string text = reader.ReadToEnd().TrimEnd('\0');
                    Append($"  Text: \"{text}\"");
                }
            }
            else
            {
                Append("  No text data in drop payload.");
            }
        }

        private async void OnSetDropDescription(object sender, RoutedEventArgs e)
        {
            await RunTest("Set Drop Description", () =>
            {
                ShellDropTarget.SetDropDescription(
                    System.Windows.DragDropEffects.Copy,
                    "Copy to TestBench",
                    "Destination");
                Append("  Drop description set (will show on next drag over target).");
                return Done;
            });
        }

        private async void OnClearDropDescription(object sender, RoutedEventArgs e)
        {
            await RunTest("Clear Drop Description", () =>
            {
                ShellDropTarget.ClearDropDescription();
                Append("  Drop description cleared.");
                return Done;
            });
        }

        private static System.Drawing.Bitmap CreateDragBitmap()
        {
            var bitmap = new System.Drawing.Bitmap(64, 64);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.Clear(System.Drawing.Color.CornflowerBlue);
                using (var font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 9))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                {
                    g.DrawString("Drag", font, brush, 10, 22);
                }
            }
            return bitmap;
        }

        private async void OnClear(object sender, RoutedEventArgs e)
        {
            OutputBox.Clear();
        }
    }
}
