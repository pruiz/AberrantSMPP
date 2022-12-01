using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;
using AberrantSMPP;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using AberrantSMPP.Exceptions;
using AberrantSMPP.Packet;
using System.Threading;
using AberrantSMPP.EventObjects;
using System.Security.Authentication;

namespace TestClient
{
	internal abstract class SMPPClientTestsBase : TestBase<SMPPClient>
	{
		protected SMPPClientTestsBase(Type declaringType) : base(declaringType) { }

		protected override void DisposeClients()
		{
			//Log("==> Disconnecting..");
			//foreach (var client in _clients.Values)
			//	client.Stop();
			Log("==> Disposing..");
			foreach (var client in _clients.Values)
				client.Dispose();
		}

		protected override SMPPClient BuildClient(
			string systemId = "client", 
			string host = "smppsim.smsdaemon.test",
			ushort port = 12000,
            SslProtocols supportedSslProtocols = SslProtocols.None,
			bool disableCheckCertificateRevocation = true)
		{
			var client = new SMPPClient(
				host: host, port: port, connectTimeout: TimeSpan.FromSeconds(30),
				supportedSslProtocols: supportedSslProtocols, 
				disableCheckCertificateRevocation: disableCheckCertificateRevocation);
			client.SystemId = "client";
			client.Password = "password";
			client.EnquireLinkInterval = TimeSpan.FromSeconds(25);
			client.BindType = AberrantSMPP.Packet.Request.SmppBind.BindingType.BindAsTransceiver;
			client.NpiType = AberrantSMPP.Packet.Pdu.NpiType.ISDN;
			client.TonType = AberrantSMPP.Packet.Pdu.TonType.International;
			client.Version = AberrantSMPP.Packet.Pdu.SmppVersionType.Version3_4;

			client.OnAlert += (s, e) => Log("Alert: " + e.Request);
			//client.OnBind += (s, e) => Log("OnBind: " + e.Request);
			client.OnBindResp += (s, e) => Log("OnBindResp: " + e.Response);
			//client.OnCancelSm += (s, e) => Log("OnCancelSm: " + e.Request);
			client.OnCancelSmResp += (s, e) => Log("OnCancelResp: " + e.Response);
			client.OnClose += (s, e) => Log("OnClose: " + e.GetType());
			//client.OnDataSm += (s, e) => Log("OnDataSm: " + e.Request);
			client.OnDataSmResp += (s, e) => Log("OnDataResp: " + e.Response);
			client.OnDeliverSm += client_OnDeliverSm;
			client.OnDeliverSmResp += (s, e) => Log("OnDeliverSmResp: " + e.Response);
			client.OnEnquireLink += client_OnEnquireLink;
			client.OnEnquireLinkResp += (s, e) => Log("OnEnquireLinkResp: " + e.Response);
			client.OnError += (s, e) => Log("OnError: " + e.ThrownException?.ToString());
			client.OnGenericNack += (s, e) => Log("OnGenericNack: " + e.Request);
			//client.OnQuerySm += (s, e) => Log("OnQuerySm: " + e.Request);
			client.OnQuerySmResp += (s, e) => Log("OnQuerySmResp: " + e.Response);
			//client.OnReplaceSm += (s, e) => Log("OnReplaceSm: " + e.Request);
			client.OnReplaceSmResp += (s, e) => Log("OnReplaceSmResp: " + e.Response);
			//client.OnSubmitMulti += (s, e) => Log("OnSubmitMulti: " + e.Request);
			client.OnSubmitMultiResp += (s, e) => Log("OnSubmitMultiResp: " + e.Response);
			//client.OnSubmitSm += (s, e) => Log("OnSubmitSm: " + e.Request);
			client.OnSubmitSmResp += (s, e) => Log("OnSubmitSmResp: " + e.Response);
			//client.OnUnbind += (s, e) => Log("OnUnbind: " + e.Request);
			client.OnUnboundResp += (s, e) => Log("OnUnboundResp: " + e.Response);
			client.OnClientStateChanged += (s, e) => Log("OnClientStateChanged: " + e.OldState + " => " + e.NewState);

			if (StartOnBuildClient)
				client.Start(TimeSpan.FromSeconds(5));

			return client;
		}

		protected void client_OnEnquireLink(object source, EnquireLinkEventArgs e)
		{
			Log("OnEnquireLink: " + e.Request);
			(source as SMPPClient)?.SendPdu(new SmppEnquireLinkResp() { SequenceNumber = e.Request.SequenceNumber });
		}

		protected void client_OnDeliverSm(object source, DeliverSmEventArgs e)
		{
			Log("OnDeliverSm: " + e.Request);
			(source as SMPPClient)?.SendPdu(new SmppDeliverSmResp() { SequenceNumber = e.Request.SequenceNumber });
		}

		protected void CreateAndSendSubmitSm(int requestPerClient, int clientId, SMPPClient client, int clientRequestId)
		{
			var txt = @"XXXXXXXXXXX de mas de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDE";
			var requestName = clientId.ToString() + "." + clientRequestId.ToString();
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
			int uniqueRequestId = NewMethod(requestPerClient, clientId, clientRequestId);
			AddSample(clientId, clientRequestId, request, response, elapsed, uniqueRequestId);
		}

		private static int NewMethod(int requestPerClient, int clientId, int clientRequestId)
		{
			return clientId * requestPerClient + clientRequestId;
		}
	}
}
