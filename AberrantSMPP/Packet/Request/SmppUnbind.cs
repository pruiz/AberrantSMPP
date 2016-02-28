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

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Class to issue an unbind request to the SMSC.
	/// </summary>
	public class SmppUnbind : SmppRequest
	{	
		protected override CommandId DefaultCommandId { get { return CommandId.Unbind; } }

		#region constructors
		
		/// <summary>
		/// Creates an unbind PDU.
		/// </summary>
		public SmppUnbind(): base()
		{}
		
		/// <summary>
		/// Creates an unbind PDU.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppUnbind(byte[] incomingBytes): base(incomingBytes)
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
