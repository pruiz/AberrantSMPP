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
using System;
using System.Text;
using System.Collections;
using AberrantSMPP.Packet;
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// This class encapsulates common attributes for submit_sm and submit_multi Pdus.
	/// </summary>
	public abstract class SmppSubmit : SmppRequest3
	{
		/// <summary>
		/// Limit of short message.
		/// </summary>
		public const int SHORT_MESSAGE_LIMIT = 160;
		
		#region private fields
		
		private SmppVersionType _ProtocolId = Pdu.SmppVersionType.Version3_4;
		private PriorityType _PriorityFlag = Pdu.PriorityType.Level1;
		private string _ScheduleDeliveryTime = string.Empty;
		private string _ValidityPeriod = string.Empty;
		private bool _ReplaceIfPresentFlag = false;
		private byte _SmDefaultMessageId = 0;
		private object _ShortMessage = null;
		
		#endregion private fields
		
		#region protected fields
		
		/// <summary>
		/// The length of the short message.
		/// </summary>
		protected byte _SmLength = 0;
		
		#endregion protected fields
		
		#region properties
		
		/// <summary>
		/// Protocol Identifier; network specific field.
		/// </summary>
		public SmppVersionType ProtocolId
		{
			get
			{
				return _ProtocolId;
			}
			set
			{
				_ProtocolId = value;
			}
		}
		
		/// <summary>
		/// The priority level of the message.
		/// </summary>
		public PriorityType PriorityFlag
		{
			get
			{
				return _PriorityFlag;
			}
			set
			{
				_PriorityFlag = value;
			}
		}
		
		/// <summary>
		/// Scheduled delivery time for the message delivery.  Set to null for immediate 
		/// delivery.  Otherwise, use YYMMDDhhmmsstnn as the format.  See section 7.1.1 of 
		/// the SMPP spec for more details.
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
		/// The validity period of this message.  Set to null to request the SMSC default 
		/// validity period.  Otherwise, use YYMMDDhhmmsstnn as the format.  See section 7.1.1 of 
		/// the SMPP spec for more details.
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
		/// Flag indicating if submitted message should replace an existing message.
		/// </summary>
		public bool ReplaceIfPresentFlag
		{
			get
			{
				return _ReplaceIfPresentFlag;
			}
			set
			{
				_ReplaceIfPresentFlag = value;
			}
		}
		
		/// <summary>
		/// Allows use of a canned message from the SMSC.  If not using an SMSC canned message, 
		/// set to 0.
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
		/// The short message to send, up to 160 octets.  If you need more length, use the 
		/// MessagePayload property.  Do not use both at the same time!  Setting this to null 
		/// will result in an empty string.  This can be either a string or a byte array; anything 
		/// else will result in an exception.
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
		
		#endregion properties
		
		#region constructors
		
		/// <summary>
		/// Groups construction tasks for subclasses.  Sets source address TON to international, 
		/// source address NPI to ISDN, source address to "", registered delivery type to none, 
		/// ESM class to 0, data coding to SMSC default, protocol ID to v3.4, priority to level 1,
		/// validity period to default, replace if present to false, default message ID to 0, 
		/// and the short message to an empty string.
		/// </summary>
		protected SmppSubmit(): base()
		{}
		
		/// <summary>
		/// Creates a new MessageLcd2 for incoming PDUs.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to decode.</param>
		protected SmppSubmit(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Creates the bytes after the destination address bytes.  This also inserts the TLV
		/// table data.  Common to both submit and submit multiple.
		/// </summary>
		/// <returns>The bytes in the Pdu before the destination address(es).</returns>
		protected ArrayList GetBytesAfterDestination()
		{
			ArrayList pdu = new ArrayList();
			pdu.Add(EsmClass);
			pdu.Add((byte)ProtocolId);
			pdu.Add((byte)PriorityFlag);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(ScheduleDeliveryTime)));
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(ValidityPeriod)));
			pdu.Add((byte)RegisteredDelivery);
			
			if(ReplaceIfPresentFlag == true)
			{
				pdu.Add((byte)0x01);
			}
			else
			{
				pdu.Add((byte)0x00);
			}
			
			pdu.Add((byte)DataCoding);
			pdu.Add(SmDefaultMessageId);
			_SmLength = PduUtil.InsertShortMessage(pdu, DataCoding, ShortMessage);
			
			pdu.TrimToSize();
			
			return pdu;
		}
	}
}
