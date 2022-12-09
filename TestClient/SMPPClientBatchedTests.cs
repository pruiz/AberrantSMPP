using System;
using System.Net.Mail;

using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace TestClient
{
	internal class SMPPClientBatchedTests : TestBase<SMPPClient>
	{
		public SMPPClientBatchedTests() : base(typeof(SMPPClientBatchedTests), startClientOnCreate: true) { }
		protected SMPPClientBatchedTests(Type declaringType, bool startClientOnCreate) 
			: base(declaringType, startClientOnCreate)
		{
		}

		protected override ISmppClient CreateClient(string name)
		{
			return new SMPPClient("smppsim.smsdaemon.test", 12000);
		}

		protected override void Configure(SMPPClient client)
		{
			base.Configure(client);

			client.ConnectTimeout = TimeSpan.FromSeconds(5);
			client.OnClientStateChanged += (s, e) => _log.Debug("OnClientStateChanged: " + e.OldState + " => " + e.NewState);
		}

		protected override SmppResponse SendAndWait(SMPPClient client, SmppRequest request)
		{
			return client.SendAndWait(request);
		}

		protected override uint SendPdu(SMPPClient client, Pdu packet)
		{
			return client.SendPdu(packet);
		}

		protected override void StartClient(SMPPClient client)
		{
			client.Start();
		}

		protected override bool IsClientReady(SMPPClient client)
		{
			return client.State == SMPPClient.States.Bound;
		}
	}
}
