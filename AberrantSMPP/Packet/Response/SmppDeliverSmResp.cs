/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 * Copyright (C) 2010, 2011 Pablo Ruiz García <pruiz@crt0.net>
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
using AberrantSMPP.Packet;

namespace AberrantSMPP.Packet.Response
{
	/// <summary>
	/// This class defines an ESME originated deliver_sm_resp.
	/// </summary>
	public class SmppDeliverSmResp : SmppResponse
	{
		protected override CommandId DefaultCommandId { get { return CommandId.deliver_sm_resp; } }

		#region constructors
		
		/// <summary>
		/// Creates a deliver_sm_resp Pdu.  Sets command status and command ID.
		/// </summary>
		public SmppDeliverSmResp(): base()
		{}
		
		/// <summary>
		/// Creates a deliver_sm_resp Pdu.  Sets command status and command ID.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes from the ESME.</param>
		public SmppDeliverSmResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
		
		protected override void AppendPduData(ArrayList pdu)
		{
			//the message_id is unused and is always set to null
			pdu.Add((byte)0x00);
		}
		
		/// <summary>
		/// Decodes the deliver_sm response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;

			//fill the TLV table if applicable
			TranslateTlvDataIntoTable(remainder, 1);
		}
	}
}
