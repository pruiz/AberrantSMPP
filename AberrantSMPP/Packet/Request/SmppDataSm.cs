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
using AberrantSMPP.Utility;
using System.Collections;
using AberrantSMPP.Packet;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// This command is used to transfer data between the SMSC and the ESME(and can be
	/// used by both).  This is an alternative to the submit_sm and deliver_sm commands.
	/// </summary>
	public class SmppDataSm : SmppRequest3
	{
		private TonType _DestinationAddressTon = Pdu.TonType.International;
		private NpiType _DestinationAddressNpi = Pdu.NpiType.ISDN;
		private string _DestinationAddress = string.Empty;
		
		#region mandatory parameters
		protected override CommandId DefaultCommandId { get { return CommandId.data_sm; } }

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
		
		#endregion mandatory parameters
		
		#region optional params
		
		/// <summary>
		/// The correct network associated with the originating device.
		/// </summary>
		public NetworkType SourceNetworkType
		{
			get
			{
				return(NetworkType)GetOptionalParamBytes(
					(ushort)OptionalParamCodes.source_network_type)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.source_network_type, new Byte[] {(byte)value});
			}
		}
		
		/// <summary>
		/// The correct bearer type for the delivering the user data to the destination.
		/// </summary>
		public BearerType SourceBearerType
		{
			get
			{
				return(BearerType)GetOptionalParamBytes(
					(ushort)OptionalParamCodes.source_bearer_type)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.source_bearer_type, new Byte[] {(byte)value});
			}
		}
		
		/// <summary>
		/// The telematics identifier associated with the source.  The value part
		/// has yet to be defined in the specs as of 07/20/2004.
		/// </summary>
		public UInt16 SourceTelematicsId
		{
			get
			{
				return GetHostOrderUInt16FromTlv((ushort)Pdu.OptionalParamCodes.source_telematics_id);
			}
			
			set
			{
				if(value < UInt16.MaxValue)
				{
					SetOptionalParamBytes(
						(UInt16)Pdu.OptionalParamCodes.source_telematics_id,
						BitConverter.GetBytes(
						UnsignedNumConverter.SwapByteOrdering(value)));
				}
				else
				{
					throw new ArgumentException("source_telematics_id value too large.");
				}
			}
		}
		
		/// <summary>
		/// The correct network for the destination device.
		/// </summary>
		public NetworkType DestNetworkType
		{
			get
			{
				return(NetworkType)GetOptionalParamBytes(
					(ushort)OptionalParamCodes.dest_network_type)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.dest_network_type, new Byte[] {(byte)value});
			}
		}
		
		/// <summary>
		/// The correct bearer type for the delivering the user data to the destination.
		/// </summary>
		public BearerType DestBearerType
		{
			get
			{
				return(BearerType)GetOptionalParamBytes(
					(ushort)OptionalParamCodes.dest_bearer_type)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.dest_bearer_type, new Byte[] {(byte)value});
			}
		}
		
		/// <summary>
		/// The telematics identifier associated with the destination.  The value part
		/// has yet to be defined in the specs as of 07/20/2004.
		/// </summary>
		public UInt16 DestTelematicsId
		{
			get
			{
				return GetHostOrderUInt16FromTlv((ushort)Pdu.OptionalParamCodes.dest_telematics_id);
			}
			
			set
			{
				if(value < UInt16.MaxValue)
				{
					SetOptionalParamBytes(
						(UInt16)Pdu.OptionalParamCodes.dest_telematics_id,
						BitConverter.GetBytes(
						UnsignedNumConverter.SwapByteOrdering(value)));
				}
				else
				{
					throw new ArgumentException("dest_telematics_id value too large.");
				}
			}
		}
		
		/// <summary>
		/// If true, this indicates that there are more messages to follow for the
		/// destination SME.
		/// </summary>
		public bool MoreMessagesToSend
		{
			get
			{
				byte sendMore = GetOptionalParamBytes(
					(ushort)OptionalParamCodes.more_messages_to_send)[0];
				
				return (sendMore == 0) ? false : true;
			}
			
			set
			{
				byte sendMore;
				if(value == false)
				{
					sendMore =(byte)0x00;
				}
				else
				{
					sendMore =(byte)0x01;
				}
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.more_messages_to_send, new Byte[] {sendMore});
			}
		}
		
		/// <summary>
		/// Time to live as a relative time in seconds from submission.
		/// </summary>
		public UInt32 QosTimeToLive
		{
			get
			{
				return GetHostOrderUInt32FromTlv((ushort)Pdu.OptionalParamCodes.qos_time_to_live);
			}
			
			set
			{
				if(value < UInt32.MaxValue)
				{
					SetOptionalParamBytes(
						(ushort)Pdu.OptionalParamCodes.qos_time_to_live,
						BitConverter.GetBytes(
						UnsignedNumConverter.SwapByteOrdering(value)));
				}
				else
				{
					throw new ArgumentException("qos_time_to_live value too large.");
				}
			}
		}
		
		/// <summary>
		/// Sets the Delivery Pending Flag on delivery failure.
		/// </summary>
		public DpfResultType SetDpf
		{
			get
			{
				return(DpfResultType)GetOptionalParamBytes(
					(ushort)OptionalParamCodes.set_dpf)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.set_dpf, new Byte[] {(byte)value});
			}
		}
		
		/// <summary>
		/// SMSC message ID of message being receipted.  Should be present for SMSC
		/// Delivery Receipts and Intermediate Notifications.
		/// </summary>
		public string ReceiptedMessageId
		{
			get
			{
				return GetOptionalParamString((ushort)Pdu.OptionalParamCodes.receipted_message_id);
			}
			
			set
			{
				PduUtil.SetReceiptedMessageId(this, value);
			}
		}
		
		/// <summary>
		/// Message State.  Should be present for SMSC Delivery Receipts and Intermediate
		/// Notifications.
		/// </summary>
		public MessageStateType MessageState
		{
			get
			{
				return (MessageStateType)GetOptionalParamBytes(
					(ushort)OptionalParamCodes.message_state)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.message_state, new Byte[] {(byte)value});
			}
		}
		
		/// <summary>
		/// Network error code.  May be present for SMSC Delivery Receipts and
		/// Intermediate Notifications.  See section 5.3.2.31 for more information.
		/// </summary>
		public string NetworkErrorCode
		{
			get
			{
				return GetOptionalParamString((ushort)Pdu.OptionalParamCodes.network_error_code);
			}
			
			set
			{
				PduUtil.SetNetworkErrorCode(this, value);
			}
		}
		
		/// <summary>
		/// A user response code. The actual response codes are implementation specific.
		/// </summary>
		public byte UserResponseCode
		{
			get
			{
				return GetOptionalParamBytes(
					(ushort)OptionalParamCodes.user_response_code)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.user_response_code,new Byte[] {value});
			}
		}
		
		/// <summary>
		/// Indicates the number of messages stored in a mail box.
		/// </summary>
		public byte NumberOfMessages
		{
			get
			{
				return GetOptionalParamBytes(
					(ushort)OptionalParamCodes.number_of_messages)[0];
			}
			
			set
			{
				if(value <= 99)
				{
					SetOptionalParamBytes(
						(ushort)Pdu.OptionalParamCodes.number_of_messages, new Byte[] {value});
				}
				else
				{
					throw new ArgumentException("number_of_messages must be between 0 and 99.");
				}
			}
		}
		
		/// <summary>
		/// Indicates and controls the MS users reply method to an SMS delivery message
		/// received from the network.
		/// </summary>
		public ItsReplyTypeType ItsReplyType
		{
			get
			{
				return (ItsReplyTypeType)GetOptionalParamBytes(
					(ushort)OptionalParamCodes.its_reply_type)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(ushort)Pdu.OptionalParamCodes.its_reply_type, new Byte[] {(byte)value});
			}
		}
		
		/// <summary>
		/// Session control information for Interactive Teleservice.
		///
		/// See section 5.3.2.43 of the SMPP spec for how to set this.
		/// </summary>
		public string ItsSessionInfo
		{
			get
			{
				return GetOptionalParamString((ushort)Pdu.OptionalParamCodes.its_session_info);
			}
			
			set
			{
				PduUtil.SetItsSessionInfo(this, value);
			}
		}
		
		#endregion optional params
		
		#region constructors
		
		/// <summary>
		/// Creates a data_sm Pdu.  Sets the destination TON to international, the destination 
		/// NPI to ISDN, and the command status to 0.
		/// </summary>
		public SmppDataSm(): base()
		{}
		
		/// <summary>
		/// Creates a data_sm Pdu.  Sets the destination TON to international, the destination 
		/// NPI to ISDN, and the command status to 0.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppDataSm(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
		
		protected override void AppendPduData(ArrayList pdu)
		{
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(ServiceType)));
			pdu.Add((byte)SourceAddressTon);
			pdu.Add((byte)SourceAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SourceAddress)));
			pdu.Add((byte)DestinationAddressTon);
			pdu.Add((byte)DestinationAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(DestinationAddress)));
			pdu.Add(EsmClass);
			pdu.Add((byte)RegisteredDelivery);
			pdu.Add((byte)DataCoding);
		}
		
		/// <summary>
		/// This decodes the cancel_sm Pdu.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			#if DEBUG
			Console.WriteLine("In DecodeSmscResponse of EsmppDataSm");
			#endif
			byte[] remainder = BytesAfterHeader;
			ServiceType = SmppStringUtil.GetCStringFromBody(ref remainder);
			SourceAddressTon = (TonType)remainder[0];
			SourceAddressNpi = (NpiType)remainder[1];
			SourceAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			DestinationAddressTon = (TonType)remainder[0];
			DestinationAddressNpi = (NpiType)remainder[1];
			DestinationAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			EsmClass = remainder[0];
			RegisteredDelivery = (RegisteredDeliveryType)remainder[1];
			DataCoding = (DataCoding)remainder[2];
			
			TranslateTlvDataIntoTable(remainder, 3);
		}
	}
}
