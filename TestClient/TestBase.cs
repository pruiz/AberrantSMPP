using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AberrantSMPP;
using AberrantSMPP.EventObjects;
using AberrantSMPP.Exceptions;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace TestClient
{
	internal abstract class TestBase<TClient>
		where TClient : class, ISmppClient
	{
		private readonly Type _declaringType;
		private readonly TestStatistics _stats;
		private readonly bool _startClientOnCreate;
		private readonly Stopwatch _sw = Stopwatch.StartNew();
		protected readonly global::Common.Logging.ILog _log = null;
		protected readonly IDictionary<int, TClient> _clients = new Dictionary<int, TClient>();

		protected TestBase(Type declaringType, bool startClientOnCreate)
		{
			_declaringType = declaringType;
			_log = global::Common.Logging.LogManager.GetLogger(declaringType);
			_stats = new TestStatistics(declaringType.Name);
			_startClientOnCreate = startClientOnCreate;
		}

		protected abstract ISmppClient CreateClient(string name);
		protected abstract SmppResponse SendAndWait(TClient client, SmppRequest request);
		protected abstract uint SendPdu(TClient client, Pdu packet);
		protected abstract void StartClient(TClient client);

		protected virtual bool IsClientReady(TClient client) => true;

		protected virtual void Configure(TClient client)
		{
			client.SystemId = client.SystemId ?? "client";
			client.Password = client.Password ?? "password";
			//client.EnquireLinkInterval = TimeSpan.FromSeconds(25);
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
		}

		protected virtual void Execute(int workers, int requests)
		{
			var requestPerClient = workers * requests;
			foreach (var client in _clients)
			{
				Task.Factory.StartNew(() => {
					Parallel.ForEach(Enumerable.Range(0, workers), (_) => {
						foreach (var id in Enumerable.Range(0, requests))
						{
							CreateAndSendSubmitSm(client.Value, id);
						}
					});
				});
			}
		}

		private void Client_OnEnquireLink(object source, EnquireLinkEventArgs e)
		{
			_log.Debug("OnEnquireLink: " + e.Request);
			SendPdu(source as TClient, new SmppEnquireLinkResp() { SequenceNumber = e.Request.SequenceNumber });
		}

		private void Client_OnDeliverSm(object source, DeliverSmEventArgs e)
		{
			_log.Debug("OnDeliverSm: " + e.Request);
			SendPdu(source as TClient, new SmppDeliverSmResp() { SequenceNumber = e.Request.SequenceNumber });
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

		protected void CreateAndSendSubmitSm(TClient client, int uid)
		{
			var txt = @"XXXXXXXXXXX de mas de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDE";
			var requestName = $"{client.SystemId}.{uid:0000000000}";
			var request = CreateSubmitSm("#" + requestName + " - " + txt); //< Clone and concat clientRequestId to its message
			var start = _sw.ElapsedMilliseconds;
			SmppResponse response;
			try
			{
				response = SendAndWait(client, request);
			}
			catch (SmppRequestException srex)
			{
				response = srex.Response;
			}
			var elapsed = _sw.ElapsedMilliseconds - start;
			_stats.AddSample(client, request, response, elapsed, uid);
		}

		private void PrintResume(int workers, int requests)
		{
			while (true)
			{
				_log.Debug("Press Q to quit.");
				_log.Debug($"Press A to show all request. Count:{_stats.Count}");
				_log.Debug($"Press R to re-run. clients:{_clients.Count}, workers:{workers}, requests:{requests}");
				_log.Debug("Press any other key to Resume.");

				var key = Console.ReadKey().Key;
				if (key == ConsoleKey.Q)
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

				client = CreateClient($"client-{clientId}") as TClient;
				Configure(client);

				// This is a hack.. but there is no common Start
				// for old and new communicator/client.
				if (_startClientOnCreate)
				{
					StartClient(client);
				}

				_clients[clientId] = client;
			}

			if (_startClientOnCreate)
			{
				_log.Info("Waiting for clients to be ready..");

				var clients = _clients.Values.Cast<TClient>().ToArray();
				while (!clients.All(IsClientReady))
				{
					_log.Info("Waiting for clients to be ready...");
					Thread.Sleep(1000);
				}
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
			var reqtotal = clients * workers * requests;

			_log.DebugFormat("name:{0}, clients:{1}, workers:{2}, requests:{3} total:{4}",
				_declaringType.Name, clients, workers, requests, reqtotal);

			RecreateClients(clients);
			Execute(workers, requests);
			DisposeClients();
		}
	}
}
