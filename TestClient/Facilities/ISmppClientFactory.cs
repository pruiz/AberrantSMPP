using System.Security.Authentication;

namespace TestClient.Facilities
{
	internal interface ISmppClientFactory
	{
		ISmppClientAdapter CreateClient(
			string name, string password, string host, ushort port,
			SslProtocols supportedSslProtocols = SslProtocols.None, bool disableSslRevocationChecking = false);
	}
}
