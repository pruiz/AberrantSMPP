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

using System.Collections;
using System.Text;
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Defines an outbind response(really a request TO us)from the SMSC.
	/// </summary>
	public class SmppOutbind : SmppRequest
	{
		private string _systemId = string.Empty;
		private string _password = string.Empty;
		
		protected override CommandId DefaultCommandId { get { return CommandId.Outbind; } }

		/// <summary>
		/// The ID of the SMSC.
		/// </summary>
		public string SystemId
		{
			get
			{
				return _systemId;
			}
			
			set
			{
				_systemId = (value == null) ? string.Empty : value;
			}
		}
		
		/// <summary>
		/// Password that the ESME can use for authentication.
		/// </summary>
		public string Password
		{
			get
			{
				return _password;
			}
			
			set
			{
				_password = (value == null) ? string.Empty : value;
			}
		}
		
		#region constructors
		
		/// <summary>
		/// Creates an outbind response Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppOutbind(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates an outbind response Pdu.
		/// </summary>
		public SmppOutbind(): base()
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the outbind response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			SystemId = SmppStringUtil.GetCStringFromBody(ref remainder);
			Password = SmppStringUtil.GetCStringFromBody(ref remainder);
			//fill the TLV table if applicable
			TranslateTlvDataIntoTable(remainder);
		}
		
		protected override void AppendPduData(ArrayList pdu)
		{
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SystemId)));
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(Password)));
		}
	}
}
