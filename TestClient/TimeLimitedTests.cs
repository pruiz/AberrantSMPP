using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using TestClient.Facilities;

namespace TestClient
{
	internal class TimeLimitedTests : BatchedTests
	{
		private TimeSpan _timeLimit;

		public TimeLimitedTests(ISmppClientFactory clientFactory)
			: this(typeof(TimeLimitedTests), clientFactory)
		{

		}

		protected TimeLimitedTests(Type declaringType, ISmppClientFactory clientFactory)
			: base(declaringType, true, clientFactory)
		{
		}

		protected override void Execute(int workers, int requests)
		{
			var totalThreads = _clients.Count * workers;
			var totalRequests = totalThreads * requests;

			var threads = new List<Thread>(totalThreads);
			var threadStartList = new List<Action>(totalThreads);
			foreach (var client in _clients.Values.ToArray())
			{
				foreach (var workerId in Enumerable.Range(0, workers))
				{
					var thread = new Thread(TimedBulkSendThreadProc);
					threads.Add(thread);
					threadStartList.Add(() => thread.Start(client));
				}
			}

			// Start
			var start = _sw.ElapsedMilliseconds;
			foreach (var threadStart in threadStartList)
			{
				threadStart();
			}
			var estimatedEnd = start + _timeLimit.TotalMilliseconds;

			while (_sw.ElapsedMilliseconds < estimatedEnd)
			{
				int requestCount = _stats.Count;
				double ellapsedSeconds = TimeSpan.FromMilliseconds(_sw.ElapsedMilliseconds - start).TotalSeconds;
				int requestPerSecond = ellapsedSeconds <= 0 ? 0 : (int)(requestCount / ellapsedSeconds);
				_log.InfoFormat("Executing ... {0} request sent in {1} seconds ({2} req/sec).",
					requestCount, ellapsedSeconds, requestPerSecond);
				Thread.Sleep((int)Math.Min(1000, estimatedEnd - _sw.ElapsedMilliseconds));
			}

			// Stop
			foreach (var thread in threads)
			{
				thread.Abort();
			}
			var stop = _sw.ElapsedMilliseconds;

			PrintResume(workers, requests);
		}

		private void TimedBulkSendThreadProc(object state)
		{
			var client = (ISmppClientAdapter)state;
			foreach (var id in Enumerable.Range(0, int.MaxValue))
				CreateAndSendSubmitSm(client, id);
		}

		public new void Run(int clients, int workers, int requests)
		{
			throw new NotImplementedException($"For {this.GetType().Name} does not implement Run interface, use TimeLimitedRun instead");
		}

		internal void TimeLimitedRun(int clients, int workers, TimeSpan timeLimit)
		{
			_timeLimit = timeLimit;
			base.Run(clients, workers, int.MaxValue);
		}
	}
}
