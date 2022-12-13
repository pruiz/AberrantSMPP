using AberrantSMPP;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace TestClient.Facilities
{
	internal class SMPPCommunicatorAdapter : SMPPCommunicator, ISmppClientAdapter
	{
		bool _bound = false;
		public void Configure()
		{
			// intentionally empty
		}

		public void Start() => _bound = Bind();

		public void Stop()
		{
			Unbind();
			_bound = false;
		}

		public bool IsClientReady() => true;

		public SmppClientStatus Status
		{
			get
			{
				if (_bound)
					return SmppClientStatus.Bound;
				if (Connected)
					return SmppClientStatus.Connected;
				return SmppClientStatus.Invalid;
			}
		}
	}
}
