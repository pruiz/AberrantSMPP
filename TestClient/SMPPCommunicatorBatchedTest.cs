using System;
using System.Security.Authentication;

using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace TestClient
{
	internal class SMPPCommunicatorBatchedTest : TestBase<SMPPCommunicator>
	{
		public SMPPCommunicatorBatchedTest() : base(typeof(SMPPCommunicatorBatchedTest), startClientOnCreate: true) { }

		protected override ISmppClient CreateClient(string name)
		{
			var client = new SMPPCommunicator()
			{
				Host = "smppsim.smsdaemon.test",
				Port = 12000,
				SystemId = "client",
				Password = "password",
				EnquireLinkInterval = TimeSpan.FromSeconds(25),
				NpiType = AberrantSMPP.Packet.Pdu.NpiType.ISDN,
				TonType = AberrantSMPP.Packet.Pdu.TonType.International,
				Version = AberrantSMPP.Packet.Pdu.SmppVersionType.Version3_4,
				SupportedSslProtocols = SslProtocols.None,
				// DisableCheckCertificateRevocation = disableCheckCertificateRevocation, //FIXME: rebase master
			};

			return client;
		}

		protected override SmppResponse SendAndWait(SMPPCommunicator client, SmppRequest request)
		{
			return client.SendRequest(request);
		}

		protected override uint SendPdu(SMPPCommunicator client, Pdu packet)
		{
			return client.SendPdu(packet);
		}

		protected override void StartClient(SMPPCommunicator client)
		{
			client.Bind();
		}
	}
}
