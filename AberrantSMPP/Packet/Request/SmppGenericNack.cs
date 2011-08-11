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
 * GNU Lessert General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using AberrantSMPP.Utility;
using System.Collections;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Class to represent a generic negative acknowledgment.
	/// </summary>
	public class SmppGenericNack : SmppRequest
	{		
		protected override CommandId DefaultCommandId { get { return CommandId.generic_nack; } }

		#region constructors
		
		/// <summary>
		/// Creates a new generic NACK.  Sets the error code to 0.
		/// </summary>
		public SmppGenericNack(): base()
		{}
		
		/// <summary>
		/// Creates a new generic NACK.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes from the ESME.</param>
		public SmppGenericNack(byte[] incomingBytes): base(incomingBytes)
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
