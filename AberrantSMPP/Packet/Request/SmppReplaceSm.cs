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
using System.Collections;
using AberrantSMPP.Packet;
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Pdu to replace a previously submitted short message that hasn't been delivered.
	/// The match is based on the message_id and source addresses of the original message.
	/// </summary>
	public class SmppReplaceSm : SmppRequest2
	{
		#region private fields
		
		private string _MessageId = string.Empty;
		private string _ScheduleDeliveryTime = string.Empty;
		private string _ValidityPeriod = string.Empty;
		private byte _SmDefaultMessageId = 0;
		private object _ShortMessage = null;
		private byte _SmLength = 0;
		
		#endregion private fields
		
		#region mandatory parameters
		protected override CommandId DefaultCommandId { get { return CommandId.replace_sm; } }

		/// <summary>
		/// SMSC message ID of the message to be replaced. This must be the message ID of the 
		/// original message.
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
		
		/// <summary>
		/// New scheduled delivery time for the short message.  Set to null, if updating of the 
		/// original scheduled delivery time is not desired.
		/// </summary>
		public string ScheduleDeliveryTime
		{
			get
			{
				return _ScheduleDeliveryTime;
			}
			set
			{
				if(value != null && value != string.Empty)
				{
					if(value.Length == DATE_TIME_LENGTH)
					{
						_ScheduleDeliveryTime = value;
					}
					else
					{
						throw new ArgumentException("Scheduled delivery time not in correct format.");
					}
				}
				else
				{
					_ScheduleDeliveryTime = string.Empty;
				}
			}
		}
		
		/// <summary>
		/// New expiration time for the short message. Set to null, if updating of the original 
		/// expiration time is not required.
		/// </summary>
		public string ValidityPeriod
		{
			get
			{
				return _ValidityPeriod;
			}
			set
			{
				if(value != null && value != string.Empty)
				{
					if(value.Length == DATE_TIME_LENGTH)
					{
						_ValidityPeriod = value;
					}
					else
					{
						throw new ArgumentException("Validity period not in correct format.");
					}
				}
				else
				{
					_ValidityPeriod = string.Empty;
				}
			}
		}
		
		/// <summary>
		/// New pre-defined(canned)message identifier.  Set to 0 if not using.
		/// </summary>
		public byte SmDefaultMessageId
		{
			get
			{
				return _SmDefaultMessageId;
			}
			set
			{
				_SmDefaultMessageId = value;
			}
		}
		
		/// <summary>
		/// New short message to replace existing message.
		/// </summary>
		public object ShortMessage
		{
			get
			{
				return _ShortMessage;
			}
			set
			{
				_ShortMessage = value;
			}
		}
		
		/// <summary>
		/// The length of the short message.
		/// </summary>
		public byte SmLength
		{
			get
			{
				return _SmLength;
			}
		}
		
		#endregion mandatory parameters
		
		#region constructors
		
		/// <summary>
		/// Creates a replace_sm Pdu.  Sets source address TON to international, 
		/// source address NPI to ISDN, source address to "", registered delivery type to none,
		/// message ID, scheduled delivery time, validity period, and short message to 
		/// empty strings(NULL SMSC value).  Sets the default message ID to 0.
		/// </summary>
		public SmppReplaceSm(): base()
		{}
		
		/// <summary>
		/// Creates a replace_sm Pdu.  Sets source address TON to international, 
		/// source address NPI to ISDN, source address to "", registered delivery type to none,
		/// message ID, scheduled delivery time, validity period, and short message to 
		/// empty strings(NULL SMSC value).  Sets the default message ID to 0.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes from an ESME.</param>
		public SmppReplaceSm(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
		
		protected override void AppendPduData(ArrayList pdu)
		{
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(MessageId)));
			pdu.Add((byte)SourceAddressTon);
			pdu.Add((byte)SourceAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SourceAddress)));
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(ScheduleDeliveryTime)));	
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(ValidityPeriod)));
			pdu.Add((byte)RegisteredDelivery);
			pdu.Add(_SmDefaultMessageId);

			_SmLength = PduUtil.InsertShortMessage(pdu, DataCoding.SMSCDefault, ShortMessage);
		}
		
		/// <summary>
		/// This decodes the submit_sm Pdu.  The Pdu has basically the same format as
		/// the submit_sm Pdu, but in this case it is a response.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			MessageId = SmppStringUtil.GetCStringFromBody(ref remainder);
			SourceAddressTon =(TonType)remainder[0];
			SourceAddressNpi =(NpiType)remainder[1];
			SourceAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			ScheduleDeliveryTime = SmppStringUtil.GetCStringFromBody(ref remainder);
			ValidityPeriod = SmppStringUtil.GetCStringFromBody(ref remainder);
			RegisteredDelivery =(RegisteredDeliveryType)remainder[0];
			SmDefaultMessageId = remainder[1];
			_SmLength = remainder[2];
			ShortMessage = SmppStringUtil.GetStringFromBody(ref remainder, 3, 3 + _SmLength);
			
			TranslateTlvDataIntoTable(remainder);
		}
	}
}
