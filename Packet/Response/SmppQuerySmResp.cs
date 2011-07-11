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
using System.Text;
using RoaminSMPP.Packet;
using RoaminSMPP.Utility;

namespace RoaminSMPP.Packet.Response
{
	/// <summary>
	/// This class defines the response to a query_sm Pdu.
	/// </summary>
	public class SmppQuerySmResp : Pdu
	{
		private string _MessageId = string.Empty;
		private string _FinalDate = string.Empty;
		private MessageStateType _MessageStatus = MessageStateType.Enroute;
		private byte _ErrorCode = 0;
		
		#region mandatory parameters
		
		/// <summary>
		/// SMSC Message ID of the message whose state is being queried.
		/// </summary>
		public string MessageId
		{
			get
			{
				return _MessageId;
			}
			
			set
			{
				_MessageId = (value == null) ? string.Empty : value;
			}
		}
		
		/// <summary>
		/// Date and time when the queried message reached a final state. For messages
		/// which have not yet reached a final state this field will be null.
		/// </summary>
		public string FinalDate
		{
			get
			{
				return _FinalDate;
			}
			
			set
			{
				if(value != null && value != string.Empty)
				{
					if(value.Length == DATE_TIME_LENGTH)
					{
						_FinalDate = value;
					}
					else
					{
						throw new ArgumentException("Final date is not in the correct format.");
					}
				}
				else
				{
					_FinalDate = string.Empty;
				}
			}
		}
		
		/// <summary>
		/// Specifies the status of the queried short message.
		/// </summary>
		public MessageStateType MessageStatus
		{
			get
			{
				return _MessageStatus;
			}
			
			set
			{
				_MessageStatus = value;
			}
		}
		
		/// <summary>
		/// Holds a network error code defining the reason for failure of message delivery.
		/// </summary>
		public byte ErrorCode
		{
			get
			{
				return _ErrorCode;
			}
			
			set
			{
				_ErrorCode = value;
			}
		}
		
		#endregion mandatory parameters
		
		#region constructors
		
		/// <summary>
		/// Creates a query_sm response.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppQuerySmResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates a query_sm response.
		/// </summary>
		public SmppQuerySmResp(): base()
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the query_sm response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			MessageId = SmppStringUtil.GetCStringFromBody(ref remainder);
			FinalDate = SmppStringUtil.GetCStringFromBody(ref remainder);
			MessageStatus = (MessageStateType)remainder[0];
			ErrorCode = remainder[1];
			//fill the TLV table if applicable
			TranslateTlvDataIntoTable(remainder, 2);
		}
		
		/// <summary>
		/// Initializes this Pdu for sending purposes.
		/// </summary>
		protected override void InitPdu()
		{
			base.InitPdu();
			CommandStatus = 0;
			CommandID = CommandIdType.query_sm_resp;
		}
		
		///<summary>
		/// Gets the hex encoding(big-endian)of this Pdu.
		///</summary>
		///<return>The hex-encoded version of the Pdu</return>
		public override void ToMsbHexEncoding()
		{
			ArrayList pdu = GetPduHeader();
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(MessageId)));
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(FinalDate)));
			pdu.Add((byte)MessageStatus);
			pdu.Add(ErrorCode);
			
			PacketBytes = EncodePduForTransmission(pdu);
		}
	}
}
