using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Nekogumi.WaniKaniSharp
{
    public class TooFastWarningEventArgs : EventArgs
    {
        public int Delay { get; }
        public bool Error { get; }
        internal TooFastWarningEventArgs(int delay, bool error) { Delay = delay; Error = error; }
    }

    public interface IETagCache
    {
        EntityTagHeaderValue GetETag(Uri? uri);
        void SetETag(Uri? uri, EntityTagHeaderValue? etag);
    }

    public class EtagHttpClientHandler : HttpClientHandler
    {
        public IETagCache? ETagCache;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var etag = ETagCache?.GetETag(request.RequestUri);
            if (etag != null)
            {
                request.Headers.IfNoneMatch.Clear();
                request.Headers.IfNoneMatch.Add(etag);
            }

            var responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (responseMessage.IsSuccessStatusCode)
                ETagCache?.SetETag(responseMessage.RequestMessage?.RequestUri, responseMessage.Headers.ETag);

            return responseMessage;
        }
    }

    public class HttpConnection : IDisposable
    {
        protected readonly HttpClient Client;
        private readonly EtagHttpClientHandler Handler;
        private bool disposed;
        private readonly DateTime[]? lastCalledTimes;
        private int lastCalledTimesIndex = 0;
        private int lastCalledTimesCount = 0;
        private readonly SemaphoreSlim connectionSemaphore = new(1);

        public Uri? BaseAddress
        {
            get => Client.BaseAddress;
            set => Client.BaseAddress = value;
        }

        public HttpConnection(int rateLimit = 0, IETagCache? cache = null)
        {
            Client = new HttpClient
            (
                handler: Handler = new EtagHttpClientHandler
                {
                    ETagCache = cache,
                }, disposeHandler: true
            );
            RateLimit = rateLimit;
            if (rateLimit != 0)
                lastCalledTimes = new DateTime[RateLimit];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) Client.Dispose();
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public static readonly object FlagParameter = new { };

        public int RateLimit { get; }
        
        public event EventHandler<TooFastWarningEventArgs>? TooFastWarning;


        private async Task<T?> GetAsync<T>(CancellationToken cancellationToken
            , Func<string?, Task<T>> getter
            , string url
            , params (string name, object value)[] parameters)
        {
            var query = url;

            if (parameters != null)
            {
                var queryBuilder = new StringBuilder();
                foreach (var (name, value) in parameters)
                {
                    if (value != null)
                    {
                        queryBuilder.Append(queryBuilder.Length == 0 ? '?' : '&');
                        queryBuilder.Append(HttpUtility.UrlEncode(name));
                        if (value != FlagParameter)
                        {
                            queryBuilder.Append('=');
                            if (value is string s) queryBuilder.Append(HttpUtility.UrlEncode(s));
                            else if (value is int i) queryBuilder.Append(i);
                            else if (value is long l) queryBuilder.Append(l);
                            else if (value is bool b) queryBuilder.Append(b ? "true" : "false");
                            else if (value is DateTime dt) queryBuilder.Append(dt.ToUniversalTime().ToString("o"));
                            else if (value is IEnumerable list)
                                queryBuilder.Append(string.Join(",", from item in list.Cast<object>()
                                                                     select HttpUtility.UrlEncode(item?.ToString())));
                            else throw new NotImplementedException();
                        }
                    }
                }
                query = url + queryBuilder.ToString();
            }
            Debug.WriteLine($"{DateTime.Now}: GET " + query);
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                await connectionSemaphore.WaitAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (RateLimit > 0 && lastCalledTimes is not null)
                    {
                        var now = DateTime.Now;
                        var limit = now.AddMinutes(-1);
                        while (lastCalledTimesCount > 0
                            && lastCalledTimes[lastCalledTimesIndex % RateLimit] < limit)
                        {
                            lastCalledTimesCount--;
                            lastCalledTimesIndex++;
                        }
                        if (lastCalledTimesCount >= lastCalledTimes.Length)
                        {
                            var waitingTime = (int)(now - lastCalledTimes[lastCalledTimesIndex % RateLimit]).TotalMilliseconds + 1000;
                            Debug.WriteLine($"{DateTime.Now}: ==> BRAKE");
                            TooFastWarning?.Invoke(this, new TooFastWarningEventArgs(waitingTime, false));
                            await Task.Delay(waitingTime, cancellationToken).ConfigureAwait(false);
                            lastCalledTimesIndex++;
                            lastCalledTimesCount--;
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                        if (lastCalledTimesCount > 7 * RateLimit / 8) await Task.Delay(3000, cancellationToken);
                        else if (lastCalledTimesCount > 3 * RateLimit / 4) await Task.Delay(2000, cancellationToken);
                        else if (lastCalledTimesCount > RateLimit / 2) await Task.Delay(1000, cancellationToken);
                        lastCalledTimes[(lastCalledTimesIndex + lastCalledTimesCount++) % RateLimit] = DateTime.Now;
                    }
                    var response = await getter(query).ConfigureAwait(false);
                    Debug.WriteLine($"{DateTime.Now}: ==> RECEIVED");
                    return response;
                }
                catch (HttpRequestException e) when (RateLimit > 0 && e.Message.Contains("429"))
                {
                    Debug.WriteLine($"{DateTime.Now}: ==> TOO FAST");
                    TooFastWarning?.Invoke(this, new TooFastWarningEventArgs(60000, true));
                    await Task.Delay(60000, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (Handler.ETagCache != null && e.Message.Contains("304"))
                {
                    Debug.WriteLine($"{DateTime.Now}: ==> SAME ETAG");
                    return default;
                }
                finally
                {
                    connectionSemaphore.Release();
                }
            }
            while (true);
        }

        public Task<string?> GetStringAsync(CancellationToken cancellationToken, string url
            , params (string name, object value)[] parameters)
            => GetAsync(cancellationToken, Client.GetStringAsync, url, parameters);

        //public Task<T> GetJSONAsync<T>(CancellationToken cancellationToken, string url
        //    , params (string name, object value)[] parameters)
        //    => GetAsync(cancellationToken, Client.GetStringAsync, url, parameters)
        //        .ContinueWith(t => t.Result is null ? default : t.Result.DeserializeJSON<T>());

        public Task<byte[]?> GetByteArrayAsync(CancellationToken cancellationToken, string url
            , params (string name, object value)[] parameters)
            => GetAsync(cancellationToken, Client.GetByteArrayAsync, url, parameters);

    }

}
