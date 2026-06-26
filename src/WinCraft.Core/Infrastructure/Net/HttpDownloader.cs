using System;
using System.Net;
using System.Threading.Tasks;

namespace WinCraft.Infrastructure.Net
{
    /// <summary>
    /// Lightweight HTTP downloader wrapping <see cref="WebClient"/> with
    /// configurable timeout and TAP-based async helpers. Works on net30 and net45.
    /// </summary>
    public class HttpDownloader : WebClient
    {
        private const int DefaultTimeoutMs = 30_000;

        public int Timeout { get; set; }

        public string UserAgent
        {
            get { return Headers[HttpRequestHeader.UserAgent]; }
            set { Headers[HttpRequestHeader.UserAgent] = value; }
        }

        public HttpDownloader()
        {
            Timeout = DefaultTimeoutMs;
            Headers[HttpRequestHeader.UserAgent] = "WinCraft";
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.Timeout = Timeout;
                httpRequest.ReadWriteTimeout = Timeout;
            }
            return request;
        }

        public Task<string> FetchStringAsync(Uri uri)
        {
            var tcs = new TaskCompletionSource<string>();

            DownloadStringCompletedEventHandler handler = null;
            handler = (sender, e) =>
            {
                DownloadStringCompleted -= handler;

                if (e.Cancelled)
                    tcs.TrySetCanceled();
                else if (e.Error != null)
                    tcs.TrySetException(e.Error);
                else
                    tcs.TrySetResult(e.Result);
            };

            DownloadStringCompleted += handler;

            try
            {
                DownloadStringAsync(uri);
            }
            catch (Exception ex)
            {
                DownloadStringCompleted -= handler;
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public Task<byte[]> FetchDataAsync(Uri uri)
        {
            var tcs = new TaskCompletionSource<byte[]>();

            DownloadDataCompletedEventHandler handler = null;
            handler = (sender, e) =>
            {
                DownloadDataCompleted -= handler;

                if (e.Cancelled)
                    tcs.TrySetCanceled();
                else if (e.Error != null)
                    tcs.TrySetException(e.Error);
                else
                    tcs.TrySetResult(e.Result);
            };

            DownloadDataCompleted += handler;

            try
            {
                DownloadDataAsync(uri);
            }
            catch (Exception ex)
            {
                DownloadDataCompleted -= handler;
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }
    }
}
