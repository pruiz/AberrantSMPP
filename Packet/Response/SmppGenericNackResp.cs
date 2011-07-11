/* RoaminSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 *
 * This file is part of RoaminSMPP.
 *
 * RoaminSMPP is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, version 3 of the License.
 *
 * RoaminSMPP is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections;
using RoaminSMPP.Packet;

namespace RoaminSMPP.Packet.Response
{
	/// <summary>
	/// Represents a generic NACK sent by the SMSC.
	/// </summary>
	public class SmppGenericNackResp : Pdu
	{
		#region constructors
		
		/// <summary>
		/// Creates a generic_nack response Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppGenericNackResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates a generic_nack response Pdu.
		/// </summary>
		public SmppGenericNackResp(): base()
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the generic NACK response from the SMSC.  Since a generic
		/// NACK response contains essentially nothing other than the header,
		/// this method does nothing special.  It will grab any TLVs that are in
		/// the Pdu, however.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			TranslateTlvDataIntoTable(BytesAfterHeader);
		}
		
		/// <summary>
		/// Initializes this Pdu for sending purposes.
		/// </summary>
		protected override void InitPdu()
		{
			base.InitPdu();
			CommandStatus = 0;
			CommandID = CommandIdType.generic_nack;
		}
		
		///<summary>
		/// Gets the hex encoding(big-endian)of this Pdu.
		///</summary>
		///<return>The hex-encoded version of the Pdu</return>
		public override void ToMsbHexEncoding()
		{
			ArrayList pdu = GetPduHeader();
			
			PacketBytes = EncodePduForTransmission(pdu);
		}
	}
}
