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

namespace AberrantSMPP.Packet.Response
{
	/// <summary>
	/// Defines the replace_sm_resp Pdu.
	/// </summary>
	public class SmppReplaceSmResp : SmppResponse
	{
		protected override CommandId DefaultCommandId { get { return CommandId.ReplaceSmResp; } }

		#region constructors
		
		/// <summary>
		/// Creates a replace_sm response Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppReplaceSmResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates a replace_sm response Pdu.
		/// </summary>
		public SmppReplaceSmResp(): base()
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the replace_sm response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			TranslateTlvDataIntoTable(BytesAfterHeader);
		}
		
	}
}
