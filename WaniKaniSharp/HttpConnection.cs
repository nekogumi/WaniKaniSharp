using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Nekogumi.WaniKani.API
{
    public class TooFastWarningEventArgs : EventArgs
    {
        public int Delay { get; }
        public bool Error { get; }
        internal TooFastWarningEventArgs(int delay, bool error) { Delay = delay; Error = error; }
    }

    public interface IETagCache
    {
        Task<(string? tag, byte[]? content)> GetEntryAsync(string uri, CancellationToken cancellationToken);
        Task SetEntryAsync(string uri, string? tag, byte[] content, CancellationToken cancellationToken);
    }

    public enum CacheStrategy
    {
        Cache,
        NoCache,
        CacheOnly,
        ForceRefresh,
    }

    public class HttpConnection : IDisposable
    {

        private class EtagHttpClientHandler : HttpClientHandler
        {
            public CacheStrategy CacheStrategy;
            public EntityTagHeaderValue? eTag;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (eTag is not null)
                {
                    request.Headers.IfNoneMatch.Clear();
                    request.Headers.IfNoneMatch.Add(eTag);
                }

                var responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (responseMessage.IsSuccessStatusCode)
                    eTag = responseMessage.Headers.ETag;
                return responseMessage;
            }
        }

        internal readonly HttpClient Client;
        private readonly EtagHttpClientHandler Handler;
        private bool disposed;
        private readonly DateTime[]? lastCalledTimes;
        private int lastCalledTimesIndex = 0;
        private int lastCalledTimesCount = 0;
        private readonly SemaphoreSlim connectionSemaphore = new(1);
        private readonly IETagCache? ETagCache;

        public Uri? BaseAddress
        {
            get => Client.BaseAddress;
            set => Client.BaseAddress = value;
        }

        public HttpConnection(int rateLimit = 0, IETagCache? cache = null)
        {
            ETagCache = cache;
            Client = new HttpClient
            (
                handler: Handler = new EtagHttpClientHandler(),
                disposeHandler: true
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

        private async Task<T> GetAsync<T>(
            Func<string?, Task<T>> getter,
            Func<byte[], T> cacheReader,
            string url,
            CacheStrategy cacheStrategy,
            CancellationToken cancellationToken,
            params (string name, object? value)[] parameters)
        {
            string query = BuildQuery(url, parameters);
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                await connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var etagKey = BaseAddress + query;
                    var (etag, cacheContent) = ETagCache is not null ? await ETagCache.GetEntryAsync(etagKey, cancellationToken).ConfigureAwait(false) : (null, null);

                    if (etag is null)
                        Handler.eTag = null;
                    else
                        _ = EntityTagHeaderValue.TryParse(etag, out Handler.eTag);
                    Handler.CacheStrategy = cacheStrategy;

                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        await ApplyRateLimiter(cancellationToken).ConfigureAwait(false);

                        var response = await getter(query).ConfigureAwait(false);

                        if (ETagCache is not null)
                        {
                            byte[] bytes;
                            if (response is byte[] b)
                                bytes = b;
                            else
                                bytes = Encoding.UTF8.GetBytes(response?.ToString() ?? string.Empty);
                            await ETagCache.SetEntryAsync(etagKey, Handler.eTag?.ToString(), bytes, cancellationToken).ConfigureAwait(false);
                        }

                        return response;
                    }
                    catch (HttpRequestException e) when (RateLimit > 0 && e.Message.Contains("429"))
                    {
                        TooFastWarning?.Invoke(this, new TooFastWarningEventArgs(60000, true));
                        await Task.Delay(60000, cancellationToken).ConfigureAwait(false);
                    }
                    catch (HttpRequestException e) when (cacheContent is not null && e.Message.Contains("304"))
                    {
                        return cacheReader(cacheContent);
                    }
                }
                finally
                {
                    connectionSemaphore.Release();
                }
            }
            while (true);
        }

        private static string BuildQuery(string url, (string name, object? value)[] parameters)
        {
            var query = url;

            if (parameters is not null)
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

            return query;
        }

        private async Task ApplyRateLimiter(CancellationToken cancellationToken)
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
        }

        public Task<string> GetStringAsync(
            string url,
            params (string name, object? value)[] parameters)
            => GetStringAsync(url, CacheStrategy.Cache, CancellationToken.None, parameters);

        public Task<string> GetStringAsync(
            string url,
            CancellationToken cancellationToken,
            params (string name, object? value)[] parameters)
            => GetStringAsync(url, CacheStrategy.Cache, cancellationToken, parameters);

        public Task<string> GetStringAsync(
            string url,
            CacheStrategy cacheStrategy,
            params (string name, object? value)[] parameters)
            => GetStringAsync(url, cacheStrategy, CancellationToken.None, parameters);

        public Task<string> GetStringAsync(
            string url,
            CacheStrategy cacheStrategy,
            CancellationToken cancellationToken,
            params (string name, object? value)[] parameters)
            => GetAsync(Client.GetStringAsync,
                content => Encoding.UTF8.GetString(content),
                url, cacheStrategy, cancellationToken, parameters);

        public Task<byte[]> GetByteArrayAsync(
            string url,
            params (string name, object? value)[] parameters)
            => GetByteArrayAsync(url, CacheStrategy.Cache, CancellationToken.None, parameters);

        public Task<byte[]> GetByteArrayAsync(
            string url,
            CancellationToken cancellationToken,
            params (string name, object? value)[] parameters)
            => GetByteArrayAsync(url, CacheStrategy.Cache, cancellationToken, parameters);

        public Task<byte[]> GetByteArrayAsync(
            string url,
            CacheStrategy cacheStrategy,
            params (string name, object? value)[] parameters)
            => GetByteArrayAsync(url, cacheStrategy, CancellationToken.None, parameters);

        public Task<byte[]> GetByteArrayAsync(
            string url,
            CacheStrategy cacheStrategy,
            CancellationToken cancellationToken,
            params (string name, object? value)[] parameters)
            => GetAsync(Client.GetByteArrayAsync,
                content => content,
                url, cacheStrategy, cancellationToken, parameters);

        public Task<TJson> GetJsonAsync<TJson>(
            string endpoint,
            CacheStrategy cacheStrategy,
            CancellationToken cancellationToken,
            params (string name, object? value)[] parameters)
            => GetJsonAsync<TJson>(endpoint, cacheStrategy, null, cancellationToken, parameters);

        public async Task<TJson> GetJsonAsync<TJson>(
            string endpoint,
            CacheStrategy cacheStrategy,
            JsonSerializerOptions? jsonOptions,
            CancellationToken cancellationToken,
            params (string name, object? value)[] parameters)
        {
            string text = await GetStringAsync(endpoint, cacheStrategy, cancellationToken, parameters).ConfigureAwait(false);
            var json = JsonSerializer.Deserialize<TJson>(text, jsonOptions);
            if (json is null) throw new InvalidOperationException();
            return json;
        }
    }

}
