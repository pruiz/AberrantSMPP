using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AberrantSMPP.Exceptions
{
	/// <summary>
	/// Remote party reported an error to our request.
	/// </summary>
	[Serializable]
	public class SmppRemoteException : Exception
	{
		/// <summary>
		/// Gets or sets the command status/error code indicated by remote party.
		/// </summary>
		/// <value>The error code.</value>
		public uint ErrorCode { get; private set; }

		protected SmppRemoteException() { }
		protected SmppRemoteException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }

		public SmppRemoteException(string message, uint errorCode) : base(message) 
		{
			ErrorCode = errorCode;
		}

		public SmppRemoteException(string message, Exception inner) : base(message, inner) 
		{
		}
	}
}
