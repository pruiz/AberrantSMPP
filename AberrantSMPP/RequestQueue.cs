using System;
using System.Collections.Specialized;
using System.Runtime.Caching;
using System.Runtime.Caching.Configuration;
using System.Threading;

using AberrantSMPP.Packet.Request;

namespace AberrantSMPP
{
	internal class RequestQueue
	{
		private static readonly global::Common.Logging.ILog _log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private MemoryCache _cache;
		private readonly CacheItemPolicy _cachePolicy;
		private readonly NameValueCollection _cacheConfig;
		private readonly string _name;
		private readonly bool _throwAddExistingSequence;

		public RequestQueue(string name, TimeSpan requestExpiration, bool throwAddExistingSequence, int cacheMemoryLimitMegabytes)
		{
			_name = name;
			_throwAddExistingSequence = throwAddExistingSequence;
			_cachePolicy = new CacheItemPolicy()
			{
				// XXX: Adds 1 second of gace period to expiration.
				SlidingExpiration = requestExpiration + TimeSpan.FromSeconds(1),
				RemovedCallback = (args) => {
					if (args.RemovedReason != CacheEntryRemovedReason.Removed)
						_log.WarnFormat("Request #{0} has been {1} from cache.", args.CacheItem.Key, args.RemovedReason);
				},
			};
			_cacheConfig = new NameValueCollection(StringComparer.OrdinalIgnoreCase) {
				{ nameof(MemoryCacheElement.CacheMemoryLimitMegabytes), cacheMemoryLimitMegabytes.ToString() }
			};
			_cache = new MemoryCache(_name, _cacheConfig, true);
		}

		private static string BuildKey(uint sequence) => sequence.ToString("x");

		public bool Add(uint sequence, SmppRequest request)
		{
			if (_throwAddExistingSequence && _cache.Contains(BuildKey(sequence)))
				throw new ArgumentException("A request with the same sequence has already been added.");

			_cache.Set(BuildKey(sequence), request, _cachePolicy);
			return true;
		}

		public bool Remove(uint sequence, out SmppRequest result)
		{
			result = _cache.Remove(BuildKey(sequence)) as SmppRequest;
			return result != null;
		}

		public void Clear()
		{
			var @new = new MemoryCache(_name, _cacheConfig, true);
			var old = Interlocked.Exchange(ref _cache, @new);
			foreach (var cacheItemKvp in old)
			{
				var request = cacheItemKvp.Value as SmppRequest;
				request?.CancelResponse();
			}
			old.Dispose();
		}
	}
}
