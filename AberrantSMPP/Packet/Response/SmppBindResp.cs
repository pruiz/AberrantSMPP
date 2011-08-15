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
using System.Text;
using System.Collections;
using AberrantSMPP;
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Response
{
	/// <summary>
	/// Class to define an SMSC bind response.
	/// </summary>
	public class SmppBindResp : SmppResponse
	{
		private string _SystemId = string.Empty;

		protected override CommandId DefaultCommandId { get { return CommandId.bind_transceiver_resp; } }

		/// <summary>
		/// The ID of the SMSC.
		/// </summary>
		public string SystemId
		{
			get
			{
				return _SystemId;
			}
			
			set
			{
				_SystemId = (value == null) ? string.Empty : value;
			}
		}
		
		/// <summary>
		/// The SMPP version supported by SMSC.
		/// </summary>
		public string ScInterfaceVersion
		{
			get
			{
				return GetOptionalParamString(Pdu.OptionalParamCodes.SC_interface_version);
			}
			
			set
			{
				string version = value;
				if(value == null)
				{
					version = string.Empty;
				}
				SetOptionalParamString(Pdu.OptionalParamCodes.SC_interface_version, version);
			}
		}
		
		#region constructors
		
		/// <summary>
		/// Creates a bind response Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppBindResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates a bind_resp. Note that this sets the bind type to 
		/// transceiver so you will want to change this if you are not dealing with a 
		/// transceiver.  This also sets system type to an empty string.
		/// </summary>
		public SmppBindResp(): base()
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the bind response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			SystemId = SmppStringUtil.GetCStringFromBody(ref remainder);
			//fill the TLV table if applicable
			TranslateTlvDataIntoTable(remainder);
		}
		
		protected override void AppendPduData(ArrayList pdu)
		{
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SystemId)));
		}
	}
}
