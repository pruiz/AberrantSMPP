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
using AberrantSMPP.Packet;
	
namespace AberrantSMPP.Packet.Response
{
	/// <summary>
	/// Defines the response Pdu from an enquire_link.
	/// </summary>
	public class SmppEnquireLinkResp : SmppResponse
	{
		protected override CommandId DefaultCommandId { get { return CommandId.enquire_link_resp; } }

		#region constructors
		
		/// <summary>
		/// Creates an enquire_link Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppEnquireLinkResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates an enquire_link Pdu.
		/// </summary>
		public SmppEnquireLinkResp(): base()
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the enquire_link response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			TranslateTlvDataIntoTable(BytesAfterHeader);
		}
	}
}
