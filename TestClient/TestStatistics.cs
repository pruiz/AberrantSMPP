using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace TestClient
{
	public class TestStatistics
	{
		private static readonly string _spacer = new string('#', 80);

		private readonly global::Common.Logging.ILog _log = global::Common.Logging.LogManager.GetLogger(typeof(TestStatistics));
		private readonly ConcurrentStack<(int? taskId, int threadId, string clientId, uint uid, SmppRequest req, SmppResponse res, long elapsedMs)> _samples = new();
		private readonly string _testName;

		public TestStatistics(string testName)
		{
			_testName = testName;
		}
		public void AddSample(ISmppClient client, SmppSubmitSm request, SmppResponse response, long elapsed, int uniqueRequestId)
		{
			_log.DebugFormat("Sending request {0} to client {1} => {2} (elapsed {3} ms)", request.SequenceNumber, client.SystemId, uniqueRequestId, elapsed);
			_samples.Push((Task.CurrentId, Thread.CurrentThread.ManagedThreadId, client.SystemId, request.SequenceNumber, request, response, elapsed));
		}

		public int Count => _samples.Count;

		public void Print(bool printSamples)
		{
			var statuses = new Dictionary<CommandStatus, int>();

			long okElapsed = 0, okCount = 0;
			long errorElapsed = 0, errorCount = 0;
			long totalElapsed = 0, totalCount = 0;
			foreach (var tuple in _samples)
			{
				if (printSamples)
				{
					var rowid = $"{tuple.clientId}:{tuple.uid}";
					_log.Debug(string.Format(
						"#{0}, Elapsed:{1,8} ms, ClientId:{2,4}, ClientRequestId:{3,5}, " +
						"Task:{4,4}, ThreadId:{5,3}, Request->SequenceNumber:{6,4}, Response->CommandStatus:{7}",
						rowid, tuple.elapsedMs, tuple.clientId, tuple.uid,
						tuple.taskId.GetValueOrDefault(), tuple.threadId, tuple.req.SequenceNumber, tuple.res.CommandStatus));
				}
				totalElapsed += tuple.elapsedMs;

				statuses.TryGetValue(tuple.res.CommandStatus, out int statusCount);
				statuses[tuple.res.CommandStatus] = statusCount + 1;
				++totalCount;

				if (tuple.res.CommandStatus == CommandStatus.ESME_ROK)
				{
					okElapsed += tuple.elapsedMs;
					okCount += 1;
				}
				else
				{
					errorElapsed += tuple.elapsedMs;
					errorCount += 1;
				}
			}

			var sb = new StringBuilder(string.Empty, 2000);
			sb.AppendLine(_spacer);
			sb.AppendLine(_testName);
			sb.AppendLine(_spacer);
			sb.Append("Total Statuses : ").AppendFormat("{0,12}", statuses.Count).AppendLine(" items."); ;
			foreach (var status in statuses)
			{
				sb.Append("Status ")
					.AppendFormat("{0,20}", status.Key)
					.Append(" :  ")
					.AppendFormat("{1,4}", status.Value)
					.AppendLine(" times.");
			}
			statuses.Clear();

			sb.AppendLine(_spacer);
			AppendStats(sb, "All", totalElapsed, totalCount);
			AppendStats(sb, "Ok", okElapsed, okCount);
			AppendStats(sb, "Error", errorElapsed, errorCount);
			sb.AppendLine(_spacer);
			_log.Debug(sb.ToString());
		}

		private static void AppendStats(StringBuilder sb, string group, long elapsed, long count)
		{
			var median = count == 0 ? 0 : elapsed / count;
			var spacer = new string(' ', 10 - group.Length);
			AppendStat(sb, group, "Elapsed Total", elapsed, " ms.");
			AppendStat(sb, group, "Requests Count", count, " items.");
			AppendStat(sb, group, "Elapsed Media", median, " ms.");
		}

		private static void AppendStat(StringBuilder sb, string group, string title, long value, string units)
		{
			sb.Append(group)
				.Append(" ")
				.Append(title)
				.Append(new string(' ', 25 - (group.Length + title.Length)))
				.Append(": ")
				.AppendFormat("{0,12}", value).AppendLine(units);
		}

		internal void Reset()
		{
			_samples.Clear();
		}
	}
}
