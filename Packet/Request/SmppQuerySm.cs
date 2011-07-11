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
using System.Text;
using RoaminSMPP.Packet;
using System.Collections;
using RoaminSMPP.Utility;

namespace RoaminSMPP.Packet.Request
{
	/// <summary>
	/// This class defines a query_sm ESME originated Pdu.
	/// </summary>
	public class SmppQuerySm : MessageLcd6
	{
		private string _MessageId = string.Empty;
		
		/// <summary>
		/// The ID of the message.
		/// </summary>
		public string MessageId
		{
			get
			{
				return _MessageId;
			}
			set
			{
				if(value != null)
				{
					if(value.Length <= MSG_LENGTH)
					{
						_MessageId = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException(
							"Message ID must be <= " + MSG_LENGTH + " characters.");
					}
				}
				else
				{
					_MessageId = string.Empty;
				}
			}
		}
		
		#region constructors
		
		/// <summary>
		/// Creates a query_sm Pdu.  Sets source address TON to international, source address 
		/// NPI to ISDN, source address to "", and message ID to an empty string.
		/// </summary>
		public SmppQuerySm(): base()
		{}
		
		/// <summary>
		/// Creates a new SmppQuerySm for incoming PDUs.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to decode.</param>
		public SmppQuerySm(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Initializes this Pdu.
		/// </summary>
		protected override void InitPdu()
		{
			base.InitPdu();
			CommandStatus = 0;
			CommandID = CommandIdType.query_sm;
		}
		
		///<summary>
		/// Gets the hex encoding(big-endian)of this Pdu.
		///</summary>
		///<return>The hex-encoded version of the Pdu</return>
		public override void ToMsbHexEncoding()
		{
			ArrayList pdu = GetPduHeader();
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(MessageId)));
			pdu.Add((byte)SourceAddressTon);
			pdu.Add((byte)SourceAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SourceAddress)));
			PacketBytes = EncodePduForTransmission(pdu);
		}
		
		/// <summary>
		/// This decodes the query_sm Pdu.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			MessageId = SmppStringUtil.GetCStringFromBody(ref remainder);
			SourceAddressTon =(TonType)remainder[0];
			SourceAddressNpi =(NpiType)remainder[1];
			SourceAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			
			TranslateTlvDataIntoTable(remainder);
		}
	}
}
