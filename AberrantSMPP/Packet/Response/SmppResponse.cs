using System;
using System.Collections;
using System.Text;

namespace AberrantSMPP.Packet.Response
{
	/// <summary>
	/// SMPP Response PDU base class.
	/// </summary>
	public abstract class SmppResponse : Pdu
	{
		#region .ctors
		/// <summary>
		/// Creates a response Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppResponse(byte[] incomingBytes)
			: base(incomingBytes)
		{
		}

		/// <summary>
		/// Creates a response Pdu.
		/// </summary>
		public SmppResponse() : base()
		{
		}
		#endregion

		protected override void AppendPduData(ArrayList pdu)
		{
			// Do nothing..
		}
	}
}
