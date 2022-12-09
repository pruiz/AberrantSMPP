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

			var startFlag = new ManualResetEvent(false);

			var threads = new List<Thread>(totalThreads);
			var threadStartList = new List<Action>(totalThreads);
			foreach (var client in _clients.Values.ToArray())
			{
				foreach (var workerId in Enumerable.Range(0, workers))
				{
					var thread = new Thread(TimedBulkSendThreadProc);
					threads.Add(thread);
					thread.Start((client, startFlag));
				}
			}

			// Start
			var start = _sw.ElapsedMilliseconds;
			startFlag.Set();
			var estimatedEnd = start + _timeLimit.TotalMilliseconds;
			while (_sw.ElapsedMilliseconds < estimatedEnd)
			{
				var ellapsed = _sw.ElapsedMilliseconds - start;
				LogProgress(ellapsed);
				Thread.Sleep((int)Math.Min(1000, estimatedEnd - _sw.ElapsedMilliseconds));
			}

			// Stop
			foreach (var thread in threads)
			{
				thread.Abort();
			}
			var totalElapsed = _sw.ElapsedMilliseconds - start;
			LogProgress(totalElapsed);
			PrintResume(workers, requests);
		}

		private void LogProgress(long ellapsed)
		{
			int requestCount = _stats.Count;
			double ellapsedSeconds = TimeSpan.FromMilliseconds(ellapsed).TotalSeconds;
			int requestPerSecond = ellapsedSeconds <= 0 ? 0 : (int)(requestCount / ellapsedSeconds);
			_log.InfoFormat("Executing ... {0} request sent in {1} seconds ({2} req/sec).",
				requestCount, ellapsedSeconds, requestPerSecond);
		}

		private void TimedBulkSendThreadProc(object state)
		{
			(ISmppClientAdapter client, ManualResetEvent startFlag) = ((ISmppClientAdapter client, ManualResetEvent startFlag))state;

			startFlag.WaitOne();
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
