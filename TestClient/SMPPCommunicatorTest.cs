using AberrantSMPP;
using AberrantSMPP.EventObjects;
using AberrantSMPP.Exceptions;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClient
{
	internal class SMPPCommunicatorTest : TestBase<SMPPCommunicator>
	{
		public SMPPCommunicatorTest() : base(typeof(SMPPCommunicatorTest))
		{

		}

		private void CreateAndSendSubmitSm(int requestPerClient, int clientId, SMPPCommunicator client, int clientRequestId)
		{
			var txt = @"XXXXXXXXXXX de mas de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDE";
			var requestName = clientId.ToString() + "." + clientRequestId.ToString();
			var request = CreateSubmitSm("#" + requestName + " - " + txt); //< Clone and concat clientRequestId to its message
			var start = _sw.ElapsedMilliseconds;
			SmppResponse response;
			try
			{
				response = client.SendRequest(request);
			}
			catch (SmppRequestException srex)
			{
				response = srex.Response;
			}
			var elapsed = _sw.ElapsedMilliseconds - start;
			var uniqueRequestId = clientId * requestPerClient + clientRequestId;
			AddSample(clientId, clientRequestId, request, response, elapsed, uniqueRequestId);
		}

		private void client_OnSubmitSmResp(object source, SubmitSmRespEventArgs e)
		{
			Log("OnSubmitSmResp: " + e.Response);
		}

		protected override void Execute(int requestPerClient)
        {
            foreach (var client in _clients)
            {
                Task.Factory.StartNew(() =>
                {
                    Enumerable.Range(0, requestPerClient)
                        .AsParallel()
                        .ForAll((clientRequestId) =>
                        {
                            Task.Factory.StartNew(() =>
                            {
                                CreateAndSendSubmitSm(requestPerClient, client.Key, client.Value, clientRequestId);
                            });
                        });
                });
            }
        }

        protected override void DisposeClients()
        {
            Log("==> Disposing..");
            foreach (var client in _clients.Values)
                DisposeClient(client);
        }

        protected override void DisposeClient(SMPPCommunicator client)
        {
            client?.Dispose();
        }

		protected override SMPPCommunicator CreateClient(string name)
        {
			var client = new SMPPCommunicator()
			{
				Host = "smppsim.smsdaemon.test",
				Port = 12000,
				SystemId = "client",
				Password = "password",
				EnquireLinkInterval = 25, // TimeSpan.FromSeconds(25),
				NpiType = AberrantSMPP.Packet.Pdu.NpiType.ISDN,
				TonType = AberrantSMPP.Packet.Pdu.TonType.International,
				Version = AberrantSMPP.Packet.Pdu.SmppVersionType.Version3_4,
                SupportedSslProtocols = SslProtocols.None,
                // DisableCheckCertificateRevocation = disableCheckCertificateRevocation, //FIXME: rebase master
            };

			return client;
		}

		protected override void Configure(SMPPCommunicator client)
		{
			//client.OnAlert += (s, e) => Log("Alert: " + e.Request);
			//client.OnBind += (s, e) => Log("OnBind: " + e.Request);
			//client.OnBindResp += (s, e) => Log("OnBindResp: " + e.Response);
			//client.OnCancelSm += (s, e) => Log("OnCancelSm: " + e.Request);
			//client.OnCancelSmResp += (s, e) => Log("OnCancelResp: " + e.Response);
			//client.OnClose += (s, e) => Log("OnClose: " + e.GetType());
			//client.OnDataSm += (s, e) => Log("OnDataSm: " + e.Request);
			//client.OnDataSmResp += (s, e) => Log("OnDataResp: " + e.Response);
			//client.OnDeliverSm += (s, e) => Log("OnDeliverSm: " + e.Request);
			//client.OnDeliverSmResp += (s, e) => Log("OnDeliverSmResp: " + e.Response);
			//client.OnEnquireLink += (s, e) => Log("OnEnquireLink: " + e.Request);
			//client.OnEnquireLinkResp += (s, e) => Log("OnEnquireLinkResp: " + e.Response);
			//client.OnError += (s, e) => Log("OnError: " + e.ThrownException?.ToString());
			//client.OnGenericNack += (s, e) => Log("OnGenericNack: " + e.Request);
			//client.OnQuerySm += (s, e) => Log("OnQuerySm: " + e.Request);
			//client.OnQuerySmResp += (s, e) => Log("OnQuerySmResp: " + e.Response);
			//client.OnReplaceSm += (s, e) => Log("OnReplaceSm: " + e.Request);
			//client.OnReplaceSmResp += (s, e) => Log("OnReplaceSmResp: " + e.Response);
			//client.OnSubmitMulti += (s, e) => Log("OnSubmitMulti: " + e.Request);
			//client.OnSubmitMultiResp += (s, e) => Log("OnSubmitMultiResp: " + e.Response);
			//client.OnSubmitSm += (s, e) => Log("OnSubmitSm: " + e.Request);
			client.OnSubmitSmResp += client_OnSubmitSmResp;
			//client.OnUnbind += (s, e) => Log("OnUnbind: " + e.Request);
			//client.OnUnboundResp += (s, e) => Log("OnUnboundResp: " + e.Response);
		}
    }
}
