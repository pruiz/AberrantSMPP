using System;
using System.Reflection;
using System.Security.Authentication;

using AberrantSMPP;

namespace TestClient.Facilities
{
	internal class SMPPClientAdapter : SMPPClient, ISmppClientAdapter, IHasConnectDisconnect, IHasBindUnbind
	{
		private static readonly global::Common.Logging.ILog _log = global::Common.Logging.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public SMPPClientAdapter(string name, string password, string host, ushort port, SslProtocols supportedSslProtocols, bool disableSslRevocationChecking)
			: base(host, port, supportedSslProtocols, disableSslRevocationChecking)
		{
			SystemId = name;
			Password = password;
		}

		public void Configure()
		{
			ConnectTimeout = TimeSpan.FromSeconds(5);
			OnClientStateChanged += (s, e) => _log.Debug("OnClientStateChanged: " + e.OldState + " => " + e.NewState);
		}

		public bool IsClientReady() => State == SMPPClient.States.Bound;

		public SmppClientStatus Status => (SmppClientStatus)(int)State;

	}
}
