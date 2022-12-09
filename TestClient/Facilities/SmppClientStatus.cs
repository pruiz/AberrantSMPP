namespace TestClient.Facilities
{
	internal enum SmppClientStatus
	{
		Invalid = -1,
		Inactive = 0,
		Connecting,
		Connected,
		Binding,
		Bound,
		Unbinding
	}
}
