using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using FASTER.core;

namespace Nekogumi.WaniKaniSharp
{

    public record ETagCacheEntry(
        string Tag,
        DateTime FetchedAt,
        byte[] Content)
    {
        public string Text => Encoding.UTF8.GetString(Content);
        public JsonNode? Json
        {
            get
            {
                try
                {
                    return JsonNode.Parse(Text);
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    internal class ETagCacheEntrySerializer : BinaryObjectSerializer<ETagCacheEntry>
    {
        public override void Deserialize(out ETagCacheEntry obj)
        {
            var tag = reader.ReadString();
            var fetchedAt = DateTime.FromBinary(reader.ReadInt64());
            var contentSize = reader.ReadInt32();
            var content = reader.ReadBytes(contentSize);

            obj = new ETagCacheEntry(tag, fetchedAt, content);
        }

        public override void Serialize(ref ETagCacheEntry obj)
        {
            writer.Write(obj.Tag);
            writer.Write(obj.FetchedAt.ToBinary());
            writer.Write(obj.Content.Length);
            writer.Write(obj.Content);
        }
    }

    public class ETagCache : IDisposable, IETagCache
    {
        private bool disposedValue;
        private readonly FasterKVSettings<string, ETagCacheEntry> settings;
        public readonly FasterKV<string, ETagCacheEntry> store;
        //private readonly IDevice log;
        //private readonly IDevice objlog;

        public ETagCache(string path)
        {
            //log = Devices.CreateLogDevice(path + ".log");
            //objlog = Devices.CreateLogDevice(path + ".obj.log");
            settings = new(path)
            {
                TryRecoverLatest = true,
                ValueSerializer = () => new ETagCacheEntrySerializer(),
                //LogDevice = log,
                //ObjectLogDevice = objlog,
            };
            store = new(settings);

            //if (store.RecoveredVersion == 1) // did not recover
            //{
            //    // Take checkpoint so data is persisted for recovery
            //    store.TryInitiateFullCheckpoint(out _, CheckpointType.Snapshot);
            //    store.CompleteCheckpointAsync().AsTask().GetAwaiter().GetResult();
            //}
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    store.Dispose();
                    settings.Dispose();
                    //log.Dispose();
                    //objlog.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ClientSession<string, ETagCacheEntry, ETagCacheEntry, ETagCacheEntry, Empty,
                            IFunctions<string, ETagCacheEntry, ETagCacheEntry, ETagCacheEntry, Empty>> NewSession()
            => store.NewSession(new SimpleFunctions<string, ETagCacheEntry>());

        public async Task<(string? tag, byte[]? content)> GetEntryAsync(string uri, CancellationToken cancellationToken)
        {
            using var session = store.NewSession(new SimpleFunctions<string, ETagCacheEntry>());

            var (status, value) = (await session.ReadAsync(uri, token: cancellationToken).ConfigureAwait(false)).Complete();
            if (status.Found)
                return (value.Tag, value.Content);
            else
                return (null, null);
        }

        public async Task SetEntryAsync(string uri, string? tag, byte[] content, CancellationToken cancellationToken)
        {
            using var session = store.NewSession(new SimpleFunctions<string, ETagCacheEntry>());

            var key = uri;
            if (tag is null)
                await session.DeleteAsync(ref key, token: cancellationToken).ConfigureAwait(false);
            else
            {
                var value = new ETagCacheEntry(tag, DateTime.Now, content);
                await session.UpsertAsync(ref key, ref value, token: cancellationToken).ConfigureAwait(false);
            }
            await session.CompletePendingAsync(token: cancellationToken).ConfigureAwait(false);
            await store.TakeHybridLogCheckpointAsync(CheckpointType.FoldOver, cancellationToken: cancellationToken).ConfigureAwait(false);
            await store.CompleteCheckpointAsync(token: cancellationToken).ConfigureAwait(false);
            store.Log.FlushAndEvict(false);
        }

    }
}
