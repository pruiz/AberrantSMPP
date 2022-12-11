using System.Security.Authentication;

namespace TestClient.Facilities
{
	internal class SMPPClientFactory : ISmppClientFactory
	{
		public ISmppClientAdapter CreateClient(
			string name, string password, string host, ushort port,
			SslProtocols supportedSslProtocols = SslProtocols.None, bool disableSslRevocationChecking = false)
		{
			return new SMPPClientAdapter(name, password, host, port, supportedSslProtocols, disableSslRevocationChecking);
		}
	}
}
