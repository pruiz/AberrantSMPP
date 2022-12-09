using System.Security.Authentication;

namespace TestClient.Facilities
{
	internal class SMPPCommunicatorFactory : ISmppClientFactory
	{
		public ISmppClientAdapter CreateClient(string name)
		{
			ISmppClientAdapter client = new SMPPCommunicatorAdapter()
			{
				Host = "smppsim.smsdaemon.test",
				Port = 12000,
				SystemId = "client",
				Password = "password",
				SupportedSslProtocols = SslProtocols.None,
				// DisableCheckCertificateRevocation = disableCheckCertificateRevocation, //FIXME: rebase master
			};

			return client;
		}
	}
}
