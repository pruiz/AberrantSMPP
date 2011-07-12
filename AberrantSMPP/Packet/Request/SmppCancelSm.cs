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
using AberrantSMPP;
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Cancels one or more previous short messages. A particular message or all messages
	/// for a particular source, destination and service_type can be cancelled.
	///
	/// From the SMPP spec:
	///  If the message_id is set to the ID of a previously submitted message, then
	/// provided the source address supplied by the ESME matches that of the stored
	/// message, that message will be cancelled.
	///  If the message_id is null, all outstanding undelivered messages with matching
	/// source and destination addresses given in the Pdu are cancelled. If provided,
	/// service_type is included in this matching.
	/// Where the original submit_sm, data_sm or submit_multi source address was
	/// defaulted to null, then the source address in the cancel_sm command should also
	/// be null.
	/// </summary>
	public class SmppCancelSm : MessageLcd6
	{
		#region private fields
		
		private string _MessageId = string.Empty;
		private string _ServiceType = string.Empty;
		private TonType _DestinationAddressTon = Pdu.TonType.International;
		private NpiType _DestinationAddressNpi = Pdu.NpiType.ISDN;
		private string _DestinationAddress = string.Empty;
		
		#endregion private fields
		
		#region properties
		
		/// <summary>
		/// Message ID of the message to be cancelled. This must be the SMSC assigned Message 
		/// ID of the original message.  Set to null if cancelling a group of messages.
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
		/// Set to indicate SMS Application service, if cancellation of a group of application 
		/// service messages is desired.  Otherwise set to null.
		/// </summary>
		public string ServiceType
		{ 
			get 
			{ 
				return _ServiceType; 
			} 
			set 
			{
				if(value != null)
				{
					if(value.Length <= SERVICE_TYPE_LENGTH)
					{
						_ServiceType = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException(
							"Service Type must be <= " + SERVICE_TYPE_LENGTH + " characters.");
					}
				}
				else
				{
					_ServiceType = string.Empty;
				}
			} 
		}
		
		/// <summary>
		/// Type of number of destination SME address of the message(s)to be cancelled.  
		/// This must match that supplied in the original message submission request.  
		/// This can be set to null if the message ID is provided.
		/// </summary>
		public TonType DestinationAddressTon
		{ 
			get 
			{ 
				return _DestinationAddressTon; 
			} 
			set 
			{
				_DestinationAddressTon = value; 
			} 
		}
		
		/// <summary>
		/// Numbering Plan Indicator of destination SME address of the message(s))to be 
		/// cancelled.  This must match that supplied in the original message submission request.  
		/// This can be set to null when the message ID is provided.
		/// </summary>
		public NpiType DestinationAddressNpi
		{ 
			get 
			{ 
				return _DestinationAddressNpi; 
			} 
			set 
			{ 
				_DestinationAddressNpi = value; 
			} 
		}
		
		/// <summary>
		/// Destination address of message(s)to be cancelled.  This must match that supplied in 
		/// the original message submission request. This can be set to null when the message ID 
		/// is provided.
		/// </summary>
		public string DestinationAddress
		{ 
			get 
			{ 
				return _DestinationAddress; 
			} 
			set 
			{
				if(value != null)
				{
					if(value.Length <= ADDRESS_LENGTH)
					{
						_DestinationAddress = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException(
							"Destination Address must be <= " + ADDRESS_LENGTH + " characters.");
					}
				}
				else
				{
					_DestinationAddress = string.Empty;
				}
			} 
		}
		
		#endregion properties
		
		#region constructors
		
		/// <summary>
		/// Creates an SMPP Cancel SM Pdu.  Sets message ID and service type to empty strings 
		///(null), sets the destination TON to international and the destination NPI to ISDN, 
		/// and sets the command status to 0.
		/// </summary>
		public SmppCancelSm(): base()
		{}
		
		/// <summary>
		/// Creates an SMPP Cancel SM Pdu.  Sets message ID and service type to empty strings 
		///(null), sets the destination TON to international and the destination NPI to ISDN, 
		/// and sets the command status to 0.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppCancelSm(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Initializes this Pdu.
		/// </summary>
		protected override void InitPdu()
		{
			base.InitPdu();
			CommandStatus = 0;
			CommandID = CommandIdType.cancel_sm;
		}
		
		///<summary>
		/// Gets the hex encoding(big-endian)of this Pdu.
		///</summary>
		///<return>The hex-encoded version of the Pdu</return>
		public override void ToMsbHexEncoding()
		{
			ArrayList pdu = GetPduHeader();
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(ServiceType)));
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(MessageId)));
			pdu.Add((byte)SourceAddressTon);
			pdu.Add((byte)SourceAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SourceAddress)));
			pdu.Add((byte)DestinationAddressTon);
			pdu.Add((byte)DestinationAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(DestinationAddress)));
			
			PacketBytes = EncodePduForTransmission(pdu);
		}
		
		/// <summary>
		/// This decodes the cancel_sm Pdu.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			ServiceType = SmppStringUtil.GetCStringFromBody(ref remainder);
			MessageId = SmppStringUtil.GetCStringFromBody(ref remainder);
			SourceAddressTon =(TonType)remainder[0];
			SourceAddressNpi =(NpiType)remainder[1];
			SourceAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			DestinationAddressTon =(TonType)remainder[0];
			DestinationAddressNpi =(NpiType)remainder[1];
			DestinationAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			
			TranslateTlvDataIntoTable(remainder);
		}
	}
}
