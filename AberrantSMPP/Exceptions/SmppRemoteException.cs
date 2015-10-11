using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace AberrantSMPP.Exceptions
{
	/// <summary>
	/// Remote party reported an error to our request.
	/// </summary>
	[Serializable]
	public class SmppRemoteException : Exception
	{
		public SmppRequest Request { get; set; }
		public SmppResponse Response { get; set; }
		public CommandStatus CommandStatus { get; set; }

		protected SmppRemoteException() { }
		protected SmppRemoteException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }

		public SmppRemoteException(string message, CommandStatus status) 
			: base(message) 
		{
			CommandStatus = status;
		}

		public SmppRemoteException(string message, SmppRequest request, SmppResponse response)
			: base(message)
		{
			if (request == null) throw new ArgumentNullException("request");
			if (response == null) throw new ArgumentNullException("response");

			Request = request;
			Response = response;
			CommandStatus = response.CommandStatus;
		}
	}
}
