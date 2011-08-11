using System;
using System.Collections;
using System.Text;

namespace AberrantSMPP.Packet.Request
{
	public abstract class SmppRequest : Pdu
	{
		#region constructors
		
		/// <summary>
		/// Groups construction tasks for subclasses.  Sets source address TON to 
		/// international, source address NPI to ISDN, and source address to "".
		/// </summary>
		protected SmppRequest(): base()
		{}
		
		/// <summary>
		/// Creates a new MessageLcd6 for incoming PDUs.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to decode.</param>
		protected SmppRequest(byte[] incomingBytes) : base(incomingBytes)
		{}
		#endregion constructors

		protected override void AppendPduData(ArrayList pdu)
		{
			// Do nothing..
		}
	}
}
