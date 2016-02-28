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
using System.Text;
using System.Collections;
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
		
		private string _messageId = string.Empty;
		private string _scheduleDeliveryTime = string.Empty;
		private string _validityPeriod = string.Empty;
		private byte _smDefaultMessageId = 0;
		private object _shortMessage = null;
		private byte _smLength = 0;
		
		#endregion private fields
		
		#region mandatory parameters
		protected override CommandId DefaultCommandId { get { return CommandId.ReplaceSm; } }

		/// <summary>
		/// SMSC message ID of the message to be replaced. This must be the message ID of the 
		/// original message.
		/// </summary>
		public string MessageId
		{
			get 
			{
				return _messageId; 
			} 
			set 
			{
				if(value != null)
				{
					if(value.Length <= MsgLength)
					{
						_messageId = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException(
							"Message ID must be <= " + MsgLength + " characters.");
					}
				}
				else
				{
					_messageId = string.Empty;
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
				return _scheduleDeliveryTime;
			}
			set
			{
				if(value != null && value != string.Empty)
				{
					if(value.Length == DateTimeLength)
					{
						_scheduleDeliveryTime = value;
					}
					else
					{
						throw new ArgumentException("Scheduled delivery time not in correct format.");
					}
				}
				else
				{
					_scheduleDeliveryTime = string.Empty;
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
				return _validityPeriod;
			}
			set
			{
				if(value != null && value != string.Empty)
				{
					if(value.Length == DateTimeLength)
					{
						_validityPeriod = value;
					}
					else
					{
						throw new ArgumentException("Validity period not in correct format.");
					}
				}
				else
				{
					_validityPeriod = string.Empty;
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
				return _smDefaultMessageId;
			}
			set
			{
				_smDefaultMessageId = value;
			}
		}
		
		/// <summary>
		/// New short message to replace existing message.
		/// </summary>
		public object ShortMessage
		{
			get
			{
				return _shortMessage;
			}
			set
			{
				_shortMessage = value;
			}
		}
		
		/// <summary>
		/// The length of the short message.
		/// </summary>
		public byte SmLength
		{
			get
			{
				return _smLength;
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
			pdu.Add(_smDefaultMessageId);

			_smLength = PduUtil.InsertShortMessage(pdu, DataCoding.SmscDefault, ShortMessage);
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
			_smLength = remainder[2];
			ShortMessage = SmppStringUtil.GetStringFromBody(ref remainder, 3, 3 + _smLength);
			
			TranslateTlvDataIntoTable(remainder);
		}
	}
}
