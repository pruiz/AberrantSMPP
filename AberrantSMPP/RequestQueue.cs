using System;
using System.Collections.Specialized;
using System.Runtime.Caching;

using AberrantSMPP.Packet.Request;

namespace AberrantSMPP
{
    public class RequestQueue
    {
        private static readonly global::Common.Logging.ILog _log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private MemoryCache _cache;
        private NameValueCollection _cacheConfig;
        private readonly string _name;
        private readonly bool _throwAddExistingSequence;
        private readonly CacheItemPolicy _cachePolicy;

        public RequestQueue(string name, TimeSpan requestExpiration, bool throwAddExistingSequence, int cacheMemoryLimitMegabytes)
        {
            _name = name;
            _throwAddExistingSequence = throwAddExistingSequence;
            _cachePolicy = new CacheItemPolicy()
            {
                //< Adds 1 second of gace period to expiration.
                SlidingExpiration = requestExpiration + TimeSpan.FromSeconds(1),
                RemovedCallback = (args) =>
                {
                    if (args.RemovedReason != CacheEntryRemovedReason.Removed)
                        _log.WarnFormat("Request #{0} has been {1} from cache.", args.CacheItem.Key, args.RemovedReason);
                },
            };
            _cacheConfig = new NameValueCollection() { { "cacheMemoryLimitMegabytes", cacheMemoryLimitMegabytes.ToString()} };
            _cache = new MemoryCache(_name, _cacheConfig, true);
        }

        public bool Add(uint sequence, SmppRequest request)
        {
            if (_throwAddExistingSequence)
                return _cache.AddOrGetExisting(BuildKey(sequence), request, _cachePolicy) != null;

            _cache.Set(BuildKey(sequence), request, _cachePolicy);
            return true;
        }

        public bool Remove(uint sequence, out SmppRequest request) =>
            (request = _cache.Remove(BuildKey(sequence)) as SmppRequest) != null;

        public void Clear()
        {
            MemoryCache oldCache = _cache;
            _cache = new MemoryCache(_name, _cacheConfig, true);
            oldCache.Dispose();
        }

        private static string BuildKey(uint sequence) => sequence.ToString("x");
    }
}
