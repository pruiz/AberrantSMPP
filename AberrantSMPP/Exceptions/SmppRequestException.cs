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
	public class SmppRequestException : Exception
	{
		public SmppRequest Request { get; set; }
		public SmppResponse Response { get; set; }
		public CommandStatus CommandStatus { get; set; }

		protected SmppRequestException() { }
		protected SmppRequestException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }

		public SmppRequestException(string message, CommandStatus status) 
			: base(message) 
		{
			CommandStatus = status;
		}

		public SmppRequestException(string message, SmppRequest request, SmppResponse response)
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
