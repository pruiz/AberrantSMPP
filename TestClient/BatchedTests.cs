using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using AberrantSMPP;
using AberrantSMPP.EventObjects;
using AberrantSMPP.Exceptions;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

using TestClient.Facilities;

namespace TestClient
{
	internal class BatchedTests
	{
		private readonly string _typeName;
		private readonly bool _startClientOnCreate;
		private readonly ISmppClientFactory _clientFactory;
		private readonly string[] _texts =
		{
			"XXXXXXXXXXX de 80 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDEFGHIJ",
			"XXXXXXXXXXX de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDEFGHIJKLMNOPQRSTUVWXYZ " +
				"XXXXXXXXXXX de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!09",
			"XXXXXXXXXXX de 240 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDEFGHIJKLMNOPQRSTUVWXYZ " +
				"XXXXXXXXXXX de 240 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDEFGHIJKLMNOPQRSTUVWXYZ " +
				"XXXXXXXXXXX de 240 caractereñ.. @€abcdefghij",
			"YYYYYYYYYYY de 80 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDEFGHIJ",
			"YYYYYYYYYYY de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDEFGHIJKLMNOPQRSTUVWXYZ " +
				"YYYYYYYYYYY de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!09",
			"ZZZZZZZZZZZ de 60 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-",
			"ZZZZZZZZZZZ de 80 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDEFGHIJ",
			"ZZZZZZZZZZZ de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDEFGHIJKLMNOPQRSTUVWXYZ " +
				"ZZZZZZZZZZZ de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!09",
		};
		protected readonly global::Common.Logging.ILog _log = null;
		protected readonly IDictionary<int, ISmppClientAdapter> _clients = new Dictionary<int, ISmppClientAdapter>();
		protected readonly Stopwatch _sw = Stopwatch.StartNew();
		protected readonly TestStatistics _stats;

		private int _textId;
		private string _testTitle = string.Empty;
		private List<Thread> _threads = new List<Thread>();

		public BatchedTests(ISmppClientFactory clientFactory)
			: this(typeof(BatchedTests), true, clientFactory)
		{
		}

		protected BatchedTests(Type declaringType, bool startClientOnCreate, ISmppClientFactory clientFactory)
		{
			_typeName = declaringType.FullName + "+" + clientFactory.GetType().Name;
			_log = global::Common.Logging.LogManager.GetLogger(declaringType);
			_stats = new TestStatistics(_typeName);
			_startClientOnCreate = startClientOnCreate;
			_clientFactory = clientFactory;
		}

		protected virtual void Configure(ISmppClientAdapter client)
		{
			client.SystemId = client.SystemId ?? "client";
			client.Password = client.Password ?? "password";
			client.EnquireLinkInterval = TimeSpan.FromSeconds(25);
			client.BindType = SmppBind.BindingType.BindAsTransceiver;
			client.NpiType = Pdu.NpiType.ISDN;
			client.TonType = Pdu.TonType.International;
			client.Version = Pdu.SmppVersionType.Version3_4;

			client.OnAlert += (s, e) => _log.Debug("Alert: " + e.Request);
			//client.OnBind += (s, e) => _log.Debug("OnBind: " + e.Request);
			client.OnBindResp += (s, e) => _log.Debug("OnBindResp: " + e.Response);
			//client.OnCancelSm += (s, e) => _log.Debug("OnCancelSm: " + e.Request);
			client.OnCancelSmResp += (s, e) => _log.Debug("OnCancelResp: " + e.Response);
			client.OnClose += (s, e) => _log.Debug("OnClose: " + e.GetType());
			//client.OnDataSm += (s, e) => _log.Debug("OnDataSm: " + e.Request);
			client.OnDataSmResp += (s, e) => _log.Debug("OnDataResp: " + e.Response);
			client.OnDeliverSm += Client_OnDeliverSm;
			client.OnDeliverSmResp += (s, e) => _log.Debug("OnDeliverSmResp: " + e.Response);
			client.OnEnquireLink += Client_OnEnquireLink;
			client.OnEnquireLinkResp += (s, e) => _log.Debug("OnEnquireLinkResp: " + e.Response);
			client.OnError += (s, e) => _log.Debug("OnError: " + e.ThrownException?.ToString());
			client.OnGenericNack += (s, e) => _log.Debug("OnGenericNack: " + e.Request);
			//client.OnQuerySm += (s, e) => _log.Debug("OnQuerySm: " + e.Request);
			client.OnQuerySmResp += (s, e) => _log.Debug("OnQuerySmResp: " + e.Response);
			//client.OnReplaceSm += (s, e) => _log.Debug("OnReplaceSm: " + e.Request);
			client.OnReplaceSmResp += (s, e) => _log.Debug("OnReplaceSmResp: " + e.Response);
			//client.OnSubmitMulti += (s, e) => _log.Debug("OnSubmitMulti: " + e.Request);
			client.OnSubmitMultiResp += (s, e) => _log.Debug("OnSubmitMultiResp: " + e.Response);
			//client.OnSubmitSm += (s, e) => _log.Debug("OnSubmitSm: " + e.Request);
			client.OnSubmitSmResp += (s, e) => _log.Debug("OnSubmitSmResp: " + e.Response);
			//client.OnUnbind += (s, e) => _log.Debug("OnUnbind: " + e.Request);
			client.OnUnboundResp += (s, e) => _log.Debug("OnUnboundResp: " + e.Response);

			client.Configure();
		}

		protected virtual ISmppClient CreateClient(string name)
		{
			return _clientFactory.CreateClient(name);
		}

		protected virtual void Execute(int workers, int requests)
		{
			var totalThreads = _clients.Count * workers;
			var totalRequests = totalThreads * requests;
			// Kill previous threads
			foreach (var thread in _threads)
			{
				thread.Abort();
			}

			_threads = new List<Thread>(totalThreads);
			var _waiters = new List<ManualResetEventSlim>();

			foreach (var client in _clients.Values.ToArray())
			{
				foreach (var workerId in Enumerable.Range(0, workers))
				{
					var thread = new Thread(BulkSendThreadProc);
					var waiter = new ManualResetEventSlim(false);
					_threads.Add(thread);
					_waiters.Add(waiter);
					thread.Start((client, requests, waiter));
				}
			}

			while (!_waiters.All(w => w.IsSet))
			{
				_log.InfoFormat("Executing ... {0} request sent of {1}.", _stats.Count, totalRequests);
				Thread.Sleep(1000);
			}

			PrintResume(workers, requests);
		}

		private void BulkSendThreadProc(object state)
		{
			(ISmppClientAdapter client, int requests, ManualResetEventSlim waiter) =
				((ISmppClientAdapter, int, ManualResetEventSlim))state;
			try
			{
				foreach (var id in Enumerable.Range(0, requests))
					CreateAndSendSubmitSm(client, id);
			}
			finally
			{
				waiter?.Set();
			}
		}

		private void Client_OnEnquireLink(object source, EnquireLinkEventArgs e)
		{
			_log.Debug("OnEnquireLink: " + e.Request);
			(source as ISmppClientAdapter).SendPdu(new SmppEnquireLinkResp() { SequenceNumber = e.Request.SequenceNumber });
		}

		private void Client_OnDeliverSm(object source, DeliverSmEventArgs e)
		{
			_log.Debug("OnDeliverSm: " + e.Request);
			(source as ISmppClientAdapter).SendPdu(new SmppDeliverSmResp() { SequenceNumber = e.Request.SequenceNumber, CommandStatus = CommandStatus.ESME_ROK });
		}

		private void DisposeClients()
		{
			_log.Debug("==> Disposing..");
			foreach (var client in _clients.Values)
				DisposeClient(client);
		}

		private void DisposeClient(ISmppClient client)
		{
			(client as IDisposable)?.Dispose();
		}

		protected void CreateAndSendSubmitSm(ISmppClientAdapter client, int uid)
		{
			var txt = @"XXXXXXXXXXX de mas de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDE";

			//txt = _texts[_textId];
			//_textId = ++_textId % _texts.Length;
			txt = _texts[0];
			_textId = ++_textId % _texts.Length;

			var requestName = $"{client.SystemId}.{uid:0000000000}";
			var request = CreateSubmitSm("#" + requestName + " - " + txt); //< Clone and concat clientRequestId to its message
			var start = _sw.ElapsedMilliseconds;
			SmppResponse response;
			try
			{
				response = client.SendAndWait(request);
			}
			catch (SmppRequestException srex)
			{
				response = srex.Response;
			}
			var elapsed = _sw.ElapsedMilliseconds - start;
			_stats.AddSample(client, request, response, elapsed, uid);
		}

		protected void PrintResume(int workers, int requests)
		{
			while (true)
			{
				StringBuilder sb = new StringBuilder(256);
				sb.AppendLine();
				sb.AppendLine(_testTitle);
				sb.AppendLine("Press X to quit.");
				sb.AppendFormat("Press A to show all request samples. Count:{0}", _stats.Count).AppendLine();
				sb.AppendFormat("Press R to re-run. clients:{0}, workers:{1}, requests:{2}", _clients.Count, workers, requests).AppendLine();
				sb.AppendLine("Press any other key to Resume.");
				_log.Info(sb.ToString());

				var key = Console.ReadKey().Key;
				if (key == ConsoleKey.Q || key == ConsoleKey.X)
					break;

				if (key == ConsoleKey.R)
				{
					_stats.Reset();
					Execute(workers, requests);
					continue;
				}

				bool printSamples = (key == ConsoleKey.A);

				_stats.Print(printSamples);
			}
		}

		protected void RecreateClients(int numberOfClients)
		{
			foreach (var clientId in Enumerable.Range(0, numberOfClients))
			{
				if (_clients.TryGetValue(clientId, out var client))
					DisposeClient(client);

				client = _clientFactory.CreateClient($"client-{clientId}");
				Configure(client);

				// This is a hack.. but there is no common Start
				// for old and new communicator/client.
				if (_startClientOnCreate)
				{
					client.Start();
				}

				_clients[clientId] = client;
			}

			if (_startClientOnCreate)
			{
				var clients = _clients.Values.Cast<ISmppClientAdapter>().ToArray();
				do
				{
					_log.Info("Waiting for clients to be ready...");
					Thread.Sleep(1000);
				} while (!clients.All(x => x.IsClientReady()));
			}
		}

		private static SmppSubmitSm CreateSubmitSm(string txt)
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

		public void Run(int clients, int workers, int requests)
		{
			_testTitle = BuildTitle(clients, workers, requests);
			_log.Info(_testTitle);

			RecreateClients(clients);
			Execute(workers, requests);
			DisposeClients();
		}

		private string BuildTitle(int clients, int workers, int requests)
		{
			var reqtotal = clients * workers * requests;
			return string.Format("name:{0}, clients:{1}, workers:{2}, requests:{3} total:{4}",
				_typeName, clients, workers, requests, reqtotal);
		}
	}
}
