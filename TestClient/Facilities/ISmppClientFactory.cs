namespace TestClient.Facilities
{
	internal interface ISmppClientFactory
	{
		ISmppClientAdapter CreateClient(string name);
	}
}
