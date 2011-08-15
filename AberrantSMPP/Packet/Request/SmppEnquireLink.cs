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
 * GNU Lessert General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using AberrantSMPP.Packet;
using System.Collections;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Defines the SMPP enquire_link Pdu.  This is basically just the header.
	/// </summary>
	public class SmppEnquireLink : SmppRequest
	{
		protected override CommandId DefaultCommandId { get { return CommandId.enquire_link; } }

		#region constructors
		
		/// <summary>
		/// Creates an enquire_link Pdu.  Sets command status and command ID.
		/// </summary>
		public SmppEnquireLink(): base()
		{}
		
		/// <summary>
		/// Creates an enquire_link Pdu.  Sets command status and command ID.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppEnquireLink(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
				
		/// <summary>
		/// This decodes the query_sm Pdu.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			
			TranslateTlvDataIntoTable(remainder);
		}
	}
}
