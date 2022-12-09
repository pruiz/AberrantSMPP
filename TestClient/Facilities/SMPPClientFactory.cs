namespace TestClient.Facilities
{
	internal class SMPPClientFactory : ISmppClientFactory
	{
		public ISmppClientAdapter CreateClient(string name)
		{
			return new SMPPClientAdapter("smppsim.smsdaemon.test", 12000);
		}
	}
}
