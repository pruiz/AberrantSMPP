using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AberrantSMPP.Packet.Response;
using AberrantSMPP;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Security.Authentication;

namespace TestClient
{
	internal abstract class TestBase<TClient>
	{
        private readonly Type _declaringType;
        protected readonly global::Common.Logging.ILog _log = null;
		protected readonly Stopwatch _sw;
        protected readonly Dictionary<int, TClient> _clients;
        private readonly ConcurrentDictionary<int, (int? taskId, int threadId, int clientId, int clientRequestId, SmppRequest req, SmppResponse res, long elapsedMs)> _samples;
        private int _numberOfClients;

        public bool StartOnBuildClient { get; protected set; } = true;

        protected TestBase(Type declaringType)
		{
            _declaringType = declaringType;
            _log = global::Common.Logging.LogManager.GetLogger(declaringType);
			_sw = Stopwatch.StartNew();
            _clients = new Dictionary<int, TClient>();
            _samples = new ConcurrentDictionary<int, (int? taskId, int threadId, int clientId, int clientRequestId, SmppRequest req, SmppResponse res, long elapsedMs)>();
		}

		protected abstract TClient CreateClient(string name);
		protected abstract void Configure(TClient client);
		protected abstract void Execute(int requestPerClient);
		protected abstract void DisposeClients();
		protected abstract void DisposeClient(TClient client);

		protected void RecreateClients()
        {
            foreach (var clientId in Enumerable.Range(0, _numberOfClients))
            {
                if (_clients.TryGetValue(clientId, out var client))
                    DisposeClient(client);

                client = CreateClient($"client-{clientId}");
                Configure(client);

                // This is a hack.. but there is no common Start
                // for old and new communicator/client.
                if (StartOnBuildClient && client is SMPPClient x) {
                    x.Start();
                }

                _clients[clientId] = client;
		    }
        }

        protected static SmppSubmitSm CreateSubmitSm(string txt)
		{
			var req = new SmppSubmitSm()
			{
				//var req = new SmppDataSm() {
				AlertOnMsgDelivery = 0x1,
				DataCoding = DataCoding.UCS2,
				SourceAddress = "WOP",
				DestinationAddress = "+34667484721",
				//DestinationAddress = "+34692471323",
				//DestinationAddress = "+34915550000",
				ValidityPeriod = "000000235959000R", // R == Time Relative to SMSC's time.
													 //EsmClass = ...
				LanguageIndicator = LanguageIndicator.Unspecified,
				//PayloadType = Pdu.PayloadTypeType.WDPMessage,
				MessagePayload = new byte[] { 0x0A, 0x0A },
				ShortMessage = txt,

				//MsValidity = Pdu.MsValidityType.StoreIndefinitely,
				//NumberOfMessages
				PriorityFlag = Pdu.PriorityType.Highest,
				//PrivacyIndicator = Pdu.PrivacyType.Nonrestricted
				RegisteredDelivery = //Pdu.RegisteredDeliveryType.OnSuccessOrFailure,
					(Pdu.RegisteredDeliveryType)0x1e,
			};

			return req;
		}

        protected void AddSample(int clientId, int clientRequestId, SmppSubmitSm request, SmppResponse response, long elapsed, int uniqueRequestId)
        {
            _log.DebugFormat("Sending request {0} to client {1} => {2} (elapsed {3} ms)", clientRequestId, clientId, uniqueRequestId, elapsed);
            _samples.TryAdd(uniqueRequestId, (Task.CurrentId, Thread.CurrentThread.ManagedThreadId, clientId, clientRequestId, request, response, elapsed));
        }

        protected virtual void PrintResume(int requestPerClient)
        {
            bool logToFile = false;
            while (true)
            {
                Log("Press Q to quit.", logToFile);
                Log($"Press A to show all request. Count:{_samples.Count}", logToFile);
                Log($"Press R to re-run. numberOfClients:{_clients.Count}, requestPerClient:{requestPerClient}", logToFile);
                Log($"Press L to toggle log to file. LogToFile {(logToFile ? "enabled" : "disabled")}", logToFile);
                Log("Press any other key to Resume.", logToFile);

                var key = Console.ReadKey().Key;
                if (key == ConsoleKey.Q)
                    break;

                if (key == ConsoleKey.L)
                {
                    logToFile = !logToFile;
                    continue;
                }

                if (key == ConsoleKey.R)
                {
                    _samples.Clear();
                    Execute(requestPerClient: requestPerClient);
                    continue;
                }

                bool printList = false;

                if (key == ConsoleKey.A)
                    printList = true;

                var statuses = new Dictionary<CommandStatus, int>();

                long okElapsed = 0;
                long okCount = 0;
                long errorElapsed = 0;
                long errorCount = 0;
                long totalElapsed = 0;
                long totalCount = 0;
                foreach (var kvp in _samples)
                {
                    var tuple = kvp.Value;
                    if (printList)
                        Log(string.Format(
                            "#{0,6:D8}, Elapsed:{1,8} ms, ClientId:{2,4}, ClientRequestId:{3,5}, " +
                            "Task:{4,4}, ThreadId:{5,3}, Request->SequenceNumber:{6,4}, Response->CommandStatus:{7}",
                            kvp.Key, tuple.elapsedMs, tuple.clientId, tuple.clientRequestId,
                            tuple.taskId.GetValueOrDefault(), tuple.threadId, tuple.req.SequenceNumber, tuple.res.CommandStatus)
                            , logToFile);
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

                Log(new string('#', 80), logToFile);
                Log(_declaringType.FullName, logToFile);
                Log(new string('#', 80), logToFile);
                Log(string.Format("Total Statuses : {0,12} items", statuses.Count), logToFile);
                foreach (var status in statuses)
                {
                    Log(string.Format("Status {0,20} :  {1,4} times", status.Key, status.Value), logToFile);
                }
                statuses.Clear();

                Log(new string('#', 80), logToFile);
                Log(string.Format("All Elapsed Total   : {0,12} ms", totalElapsed), logToFile);
                Log(string.Format("All Requests Count  : {0,12} items", totalCount), logToFile);
                Log(string.Format("All Elapsed Media   : {0,12} ms", totalCount == 0 ? 0 : totalElapsed / totalCount), logToFile);
                Log(string.Format("Ok Elapsed Total    : {0,12} ms", okElapsed), logToFile);
                Log(string.Format("Ok Requests         : {0,12} items", okCount), logToFile);
                Log(string.Format("Ok Elapsed Media    : {0,12} items", okCount == 0 ? 0 : okElapsed / okCount), logToFile);
                Log(string.Format("Error Elapsed Total : {0,12} ms", errorElapsed), logToFile);
                Log(string.Format("Error Requests      : {0,12} items", errorCount), logToFile);
                Log(string.Format("Error Elapsed Media : {0,12} ms", errorCount == 0 ? 0 : errorElapsed / errorCount), logToFile);
            }
        }

        protected void Log(string text, bool logToFile = true)
		{
			if (logToFile)
				_log.Debug(text);
			else
				Console.WriteLine(text);
		}

		public void Run(int numberOfClients, int requestPerClient)
		{
			_numberOfClients = numberOfClients;
			var totalRequests = numberOfClients * requestPerClient;

			_log.DebugFormat("name:{0}, numberOfClients:{1}, requestPerClient:{2}, totalRequests:{3}",
				_declaringType.Name, _numberOfClients, requestPerClient, totalRequests);

			RecreateClients();

			if (totalRequests != 0)
				Execute(requestPerClient);

			PrintResume(requestPerClient);

			DisposeClients();
		}

	}
}
