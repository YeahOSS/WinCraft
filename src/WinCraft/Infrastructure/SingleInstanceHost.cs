using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace WinCraft.Infrastructure
{
    /// <summary>
    /// Single-instance host using a named <see cref="Mutex"/> and
    /// <see cref="IpcChannel"/>, mirroring the mechanism in
    /// <c>WindowsFormsApplicationBase</c> without the WinForms dependency.
    /// </summary>
    internal sealed class SingleInstanceHost : IDisposable
    {
        private readonly Application _app;
        private readonly Dispatcher _dispatcher;
        private readonly string _mutexName;
        private readonly string _ipcPortName;
        private readonly string _ipcUri;
        private readonly SendOrPostCallback _startNextInstanceCallback;
        private Mutex _mutex;
        private IpcChannel _channel;
        private volatile bool _disposed;

        public event EventHandler<InstanceActivatedEventArgs> StartupNextInstance;

        public SingleInstanceHost(Application app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dispatcher = app.Dispatcher;
            _startNextInstanceCallback = OnStartupNextInstanceCallback;
            var identity = ProductInfo.ProductName;
            _mutexName = $"WinCraft.SingleInstance.{identity}";
            _ipcPortName = $"WinCraft.SingleInstance.{identity}.ipc";
            _ipcUri = $"WinCraft.SingleInstance.{identity}.remote";
        }

        public void Run(string[] args)
        {
            _mutex = new Mutex(true, _mutexName, out bool createdNew);

            if (!createdNew)
            {
                _mutex.Close();
                _mutex = null;
                ForwardCommandLine(args ?? []);
                return;
            }

            StartIpcServer();
            try
            {
                _app.Run(_app.MainWindow);
            }
            finally
            {
                StopIpcServer();
            }
        }

        private void ForwardCommandLine(string[] args)
        {
            try
            {
                var url = $"ipc://{_ipcPortName}/{_ipcUri}";
                var remote = (IRemoteInstance)Activator.GetObject(typeof(IRemoteInstance), url);
                remote.Activate(args);
            }
            catch
            {
                // First instance not listening — exit silently.
            }
        }

        private void StartIpcServer()
        {
            var serverProvider = new BinaryServerFormatterSinkProvider
            {
                TypeFilterLevel = TypeFilterLevel.Full
            };

            var properties = new Hashtable
            {
                ["portName"] = _ipcPortName,
                ["authorizedGroup"] = "Everyone"
            };

            _channel = new IpcChannel(properties, null, serverProvider);
            ChannelServices.RegisterChannel(_channel, ensureSecurity: false);

            var remoteInstance = new RemoteInstance(this);
            RemotingServices.Marshal(remoteInstance, _ipcUri);
        }

        private void StopIpcServer()
        {
            try
            {
                if (_channel != null)
                {
                    ChannelServices.UnregisterChannel(_channel);
                    _channel = null;
                }
            }
            catch
            {
                // Best-effort cleanup on shutdown.
            }
        }

        /// <summary>
        /// Called on an IPC thread; dispatches to the UI thread via
        /// <see cref="Dispatcher"/>.
        /// </summary>
        internal void OnRemoteActivated(string[] args)
        {
            if (_disposed)
                return;

            _dispatcher.BeginInvoke(DispatcherPriority.Normal, _startNextInstanceCallback, args);
        }

        private void OnStartupNextInstanceCallback(object state)
        {
            if (_disposed)
                return;

            var args = (string[])state;
            StartupNextInstance?.Invoke(this, new InstanceActivatedEventArgs(args));
        }

        public void Dispose()
        {
            _disposed = true;
            StopIpcServer();
            _mutex?.Close();
        }
    }

    internal interface IRemoteInstance
    {
        [OneWay]
        void Activate(string[] args);
    }

    internal sealed class RemoteInstance : MarshalByRefObject, IRemoteInstance
    {
        private readonly SingleInstanceHost _host;
        private readonly WindowsIdentity _originalUser;

        internal RemoteInstance(SingleInstanceHost host)
        {
            _host = host;
            _originalUser = WindowsIdentity.GetCurrent();
        }

        public void Activate(string[] args)
        {
            if (_originalUser.User != WindowsIdentity.GetCurrent().User)
                return;

            _host.OnRemoteActivated(args ?? []);
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }

    public class InstanceActivatedEventArgs(string[] commandLine) : EventArgs
    {
        public string[] CommandLine { get; } = commandLine ?? [];
    }
}
