using System;
using System.ServiceProcess;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Starts the TrustedInstaller service on demand.
    /// </summary>
    internal static class TrustedInstallerService
    {
        public static void EnsureRunning()
        {
            using var serviceController = new ServiceController("TrustedInstaller");
            if (serviceController.Status == ServiceControllerStatus.Running)
                return;

            if (serviceController.Status == ServiceControllerStatus.StartPending)
            {
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                return;
            }

            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
        }
    }
}
