using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AberrantSMPP.Exceptions
{
	/// <summary>
	/// Communication timeout during an SMPP transaction.
	/// </summary>
	[Serializable]
	public class SmppTimeoutException : Exception
	{
		public SmppTimeoutException() { }
		public SmppTimeoutException(string message) : base(message) { }
		public SmppTimeoutException(string message, Exception inner) : base(message, inner) { }
		protected SmppTimeoutException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}
