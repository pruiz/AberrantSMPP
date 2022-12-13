using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace TestClient.Facilities
{
	internal interface ISmppClientAdapter : ISmppClient
	{
		void Configure();
		void Start();
		//void Bind();
		void Stop();
		//void Unbind();
		//void Connect();
		//void Disconnect();
		bool IsClientReady();
		SmppClientStatus Status { get; }
	}
}
