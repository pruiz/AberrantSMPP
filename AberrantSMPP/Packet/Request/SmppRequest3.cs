/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 * Copyright (C) 2010, 2011 Pablo Ruiz Garc�a <pruiz@crt0.net>
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
using AberrantSMPP.Packet;
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Provides some common attributes for data_sm, submit_sm, and submit_multi.
	/// </summary>
	public abstract class SmppRequest3 : SmppRequest2
	{
		#region private fields

		private string _ServiceType = string.Empty;
		private byte _EsmClass = 0;
		private DataCoding _DataCoding = DataCoding.SMSCDefault;
		
		#endregion private fields
		
		#region constants
		
		private const int SAR_MIN = 1;
		private const int SAR_MAX = 255;
		
		#endregion constants
		
		#region mandatory parameters
		
		/// <summary>
		/// The service type of the message.  Null values are treated as empty strings.
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
						throw new ArgumentOutOfRangeException("Service Type must be " + 
							SERVICE_TYPE_LENGTH + " 5 characters or less.");
					}
				}
				else
				{
					_ServiceType = string.Empty;
				}
			}
		}
		
		/// <summary>
		/// Indicates Message Mode and Message Type.  See the SMSC version 3.4 specification 
		/// for details on setting this.
		/// </summary>
		public byte EsmClass
		{
			get
			{
				return _EsmClass;
			}
			set
			{
				_EsmClass = value;
			}
		}
		
		/// <summary>
		/// Defines the encoding scheme of the short message user data.
		/// </summary>
		public DataCoding DataCoding
		{
			get
			{
				return _DataCoding;
			}
			set
			{
				_DataCoding = value;
			}
		}
		
		#endregion mandatory parameters
		
		#region optional params
		
		/// <summary>
		/// The application port number associated with the source address of the message.
		/// This parameter should be present for WAP applications.
		/// </summary>
		public UInt16? SourcePort
		{
			get
			{
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.source_port);
			}
			set
			{
				if(value == null || value < UInt16.MaxValue)
				{
					SetHostOrderValueIntoTlv(OptionalParamCodes.source_port, value);
				}
				else
				{
					throw new ArgumentException("source_port value too large.");
				}
			}
		}
		
		/// <summary>
		/// The subcomponent in the destination device which created the user data.
		/// </summary>
		public AddressSubunitType? SourceAddressSubunit
		{
			get
			{
				return GetOptionalParamByte<AddressSubunitType>(OptionalParamCodes.source_addr_subunit);
			}
			set
			{
				SetOptionalParamByte(OptionalParamCodes.source_addr_subunit, value);
			}
		}
		
		/// <summary>
		/// The application port number associated with the destination address of the
		/// message.  This parameter should be present for WAP applications.
		/// </summary>
		public UInt16? DestinationPort
		{
			get
			{
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.destination_port);
			}
			set
			{
				if(value == null || value < UInt16.MaxValue)
				{
					SetHostOrderValueIntoTlv(OptionalParamCodes.destination_port, value);
				}
				else
				{
					throw new ArgumentException("destination_port value too large.");
				}
			}
		}
		
		/// <summary>
		/// The reference number for a particular concatenated short message.  Both
		/// SarTotalSegments and SarSegmentSeqnum need to be set in conjunction with this
		/// property.  In addition, this must be the same for each segment.
		/// </summary>
		public UInt16? SarMsgRefNumber
		{
			get
			{
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.sar_msg_ref_num);
			}
			set
			{
				SetHostOrderValueIntoTlv(OptionalParamCodes.sar_msg_ref_num, value);
			}
		}
		
		/// <summary>
		/// Indicates the total number of short messages within the concatenated short
		/// message.  Both SarMsgRefNumber and SarSegmentSeqNum need to be set in
		/// conjunction with this property.  In addition, this must be the same for each
		/// segment.
		/// </summary>
		public byte? SarTotalSegments
		{
			get
			{
				return GetOptionalParamByte(OptionalParamCodes.sar_total_segments);
			}
			
			set
			{
				if(value == null || (value >= SAR_MIN && value <= SAR_MAX))
				{
					SetOptionalParamByte(OptionalParamCodes.sar_total_segments, value);
				}
				else
				{
					throw new ArgumentException("sar_total_segments must be >= " + SAR_MIN + " and <= " + SAR_MAX);
				}
			}
		}
		
		/// <summary>
		/// The sequence number of a particular short message within the concatenated
		/// short message.  Both SarMsgRefNumber and SarTotalSegments need to be set in
		/// conjunction with this property.
		/// </summary>
		public byte? SarSegmentSeqnum
		{
			get
			{
				return GetOptionalParamByte(OptionalParamCodes.sar_segment_seqnum);
			}
			
			set
			{
				if(value == null || (value >= SAR_MIN && value <= SAR_MAX))
				{
					SetOptionalParamByte(OptionalParamCodes.sar_segment_seqnum, value);
				}
				else
				{
					throw new ArgumentException("sar_segment_seqnum must be >= " + SAR_MIN + " and <= " + SAR_MAX);
				}
			}
		}
		
		/// <summary>
		/// Defines the type of payload.
		/// </summary>
		public PayloadTypeType? PayloadType
		{
			get
			{
				return GetOptionalParamByte<PayloadTypeType>(OptionalParamCodes.payload_type);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.payload_type, value);
			}
		}
		
		/// <summary>
		/// Contains the extended short message user data.  Up to 64K octets can be
		/// transmitted.  The limit for this is network/SMSC dependent.
		/// </summary>
		public object MessagePayload
		{
			get
			{
				return GetOptionalParamBytes(
					OptionalParamCodes.message_payload);
			}
			
			set
			{
				PduUtil.SetMessagePayload(this, DataCoding, value);
			}
		}
		
		/// <summary>
		/// The privacy level of the message.
		/// </summary>
		public PrivacyType? PrivacyIndicator
		{
			get
			{
				return GetOptionalParamByte<PrivacyType>(OptionalParamCodes.privacy_indicator);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.privacy_indicator, value);
			}
		}
		
		/// <summary>
		/// ESME assigned message reference number.
		/// </summary>
		public UInt16? UserMessageReference
		{
			get
			{				
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.user_message_reference);
			}
			set
			{
				if(value == null || value < UInt16.MaxValue)
				{
					SetHostOrderValueIntoTlv(OptionalParamCodes.user_message_reference, value);
				}
				else
				{
					throw new ArgumentException("user_message_reference too large.");
				}
			}
		}
		
		/// <summary>
		/// Allows an indication to be provided to an MS that there are
		/// messages waiting for the subscriber on systems on the PLMN. The indication
		/// can be an icon on the MS screen or other MMI indication.
		/// See section 5.3.2.13 for details on how to set this.
		/// </summary>
		public byte? MsMsgWaitFacilities
		{
			get
			{
				return GetOptionalParamByte(OptionalParamCodes.ms_msg_wait_facilities);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.ms_msg_wait_facilities, value);
			}
		}
		
		/// <summary>
		/// Provides a MS with validity information associated with the received
		/// short message.
		/// </summary>
		public MsValidityType? MsValidity
		{
			get
			{
				return GetOptionalParamByte<MsValidityType>(OptionalParamCodes.ms_validity);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.ms_validity, value);
			}
		}
		
		/// <summary>
		/// Provides a TDMA MS station with alert tone information associated with the
		/// received short message.
		/// </summary>
		public UInt16? SmsSignal
		{
			get
			{
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.sms_signal);
			}
			
			set
			{
				SetHostOrderValueIntoTlv(OptionalParamCodes.sms_signal, value);
			}
		}
		
		/// <summary>
		/// The subcomponent in the destination device for which the user data is intended.
		/// </summary>
		public AddressSubunitType? DestinationAddrSubunit
		{
			get
			{
				return GetOptionalParamByte<AddressSubunitType>(OptionalParamCodes.dest_addr_subunit);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.dest_addr_subunit, value);
			}
		}
		
		/// <summary>
		/// Tells a mobile station to alert the user when the short message arrives at the MS.
		///
		/// Note: there is no value part associated with this parameter.
		/// Any value but null you pass in will be discarded.
		/// </summary>
		public byte? AlertOnMsgDelivery
		{
			get
			{
				try
				{
					byte[] bytes = GetOptionalParamBytes(
						OptionalParamCodes.alert_on_message_delivery);
					return bytes == null ? null : (byte?) 1;
				}
				catch(ApplicationException)
				{
					return 0;
				}
			}
			
			set
			{
				SetOptionalParamBytes(
					OptionalParamCodes.alert_on_message_delivery, value.HasValue ? new Byte[0] : null);
			}
		}
		
		/// <summary>
		/// The language of the short message.
		/// </summary>
		public LanguageIndicator? LanguageIndicator
		{
			get
			{
				return GetOptionalParamByte<LanguageIndicator>(OptionalParamCodes.language_indicator);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.language_indicator, value);
			}
		}
		
		/// <summary>
		/// Associates a display time with the short message on the MS.
		/// </summary>
		public DisplayTimeType? DisplayTime
		{
			get
			{
				return GetOptionalParamByte<DisplayTimeType>(OptionalParamCodes.display_time);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.display_time, value);
			}
		}
		
		/// <summary>
		/// Associates a call back number with the message.  See section 5.3.2.36 of the
		/// SMPP spec for details.  This must be between 4 and 19 characters in length.
		/// </summary>
		public byte[] CallbackNum
		{
			get
			{
				return GetOptionalParamBytes(OptionalParamCodes.callback_num);
			}
			
			set
			{
				PduUtil.SetCallbackNum(this, value);
			}
		}
		
		/// <summary>
		/// Controls the presentation indication and screening of the CallBackNumber at the
		/// mobile station.  You must also use the callback_num parameter with this.
		/// See section 5.3.2.37 of the SMPP spec for details in how to set this.
		/// </summary>
		public byte? CallbackNumPresInd
		{
			get
			{
				return GetOptionalParamByte(OptionalParamCodes.callback_num_pres_ind);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.callback_num_pres_ind, value);
			}
		}
		
		/// <summary>
		/// Alphanumeric display tag for call back number.  This must be less than or
		/// equal to 65 bytes in length, and should be encoded according to spec.
		/// </summary>
		public byte[] CallbackNumAtag
		{
			get
			{
				return GetOptionalParamBytes(OptionalParamCodes.callback_num_atag);
			}
			
			set
			{
				const int CALLBACK_ATAG_MAX = 65;

				if (value == null)
				{
					SetOptionalParamBytes(OptionalParamCodes.callback_num_atag, null);
				}
				else if (value.Length <= CALLBACK_ATAG_MAX)
				{
					SetOptionalParamBytes(OptionalParamCodes.callback_num_atag, value);
				}
				else
				{
					throw new ArgumentException("Callback number atag must be <= " + CALLBACK_ATAG_MAX + ".");
				}
			}
		}
		
		/// <summary>
		/// Specifies a subaddress associated with the originator of the message.
		/// See section 5.3.2.15 of the SMPP spec for details on
		/// setting this parameter.
		/// </summary>
		public byte[] SourceSubaddress
		{
			get
			{
				return GetOptionalParamBytes(OptionalParamCodes.source_subaddress);
			}
			
			set
			{
				PduUtil.SetSourceSubaddress(this, value);
			}
		}
		
		/// <summary>
		/// Specifies a subaddress associated with the receiver of the message.
		/// See section 5.3.2.15 of the SMPP spec for details on
		/// setting this parameter.
		/// </summary>
		public byte[] DestinationSubaddress
		{
			get
			{
				return GetOptionalParamBytes(OptionalParamCodes.dest_subaddress);
			}
			
			set
			{
				PduUtil.SetDestSubaddress(this, value);
			}
		}
		
		#endregion optional params
		
		#region constructors
		
		/// <summary>
		/// Groups construction tasks for subclasses.  Sets source address TON to international, 
		/// source address NPI to ISDN, source address to "", registered delivery type to none, 
		/// ESM class to 0, and data coding to SMSC default.
		/// </summary>
		protected SmppRequest3(): base()
		{}
		
		/// <summary>
		/// Creates a new MessageLcd3 for incoming PDUs.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to decode.</param>
		protected SmppRequest3(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
	}
}
