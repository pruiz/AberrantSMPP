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
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Provides some common attributes for data_sm, submit_sm, and submit_multi.
	/// </summary>
	public abstract class SmppRequest3 : SmppRequest2
	{
		#region private fields

		private string _serviceType = string.Empty;
		private byte _esmClass = 0;
		private DataCoding _dataCoding = DataCoding.SmscDefault;
		
		#endregion private fields
		
		#region constants
		
		private const int SarMin = 1;
		private const int SarMax = 255;
		
		#endregion constants
		
		#region mandatory parameters
		
		/// <summary>
		/// The service type of the message.  Null values are treated as empty strings.
		/// </summary>
		public string ServiceType
		{
			get
			{				
				return _serviceType;
			}
			
			set
			{
				if(value != null)
				{
					if(value.Length <= ServiceTypeLength)
					{
						_serviceType = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException("Service Type must be " + 
							ServiceTypeLength + " 5 characters or less.");
					}
				}
				else
				{
					_serviceType = string.Empty;
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
				return _esmClass;
			}
			set
			{
				_esmClass = value;
			}
		}
		
		/// <summary>
		/// Defines the encoding scheme of the short message user data.
		/// </summary>
		public DataCoding DataCoding
		{
			get
			{
				return _dataCoding;
			}
			set
			{
				_dataCoding = value;
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
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.SourcePort);
			}
			set
			{
				if(value == null || value < UInt16.MaxValue)
				{
					SetHostOrderValueIntoTlv(OptionalParamCodes.SourcePort, value);
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
				return GetOptionalParamByte<AddressSubunitType>(OptionalParamCodes.SourceAddrSubunit);
			}
			set
			{
				SetOptionalParamByte(OptionalParamCodes.SourceAddrSubunit, value);
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
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.DestinationPort);
			}
			set
			{
				if(value == null || value < UInt16.MaxValue)
				{
					SetHostOrderValueIntoTlv(OptionalParamCodes.DestinationPort, value);
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
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.SarMsgRefNum);
			}
			set
			{
				SetHostOrderValueIntoTlv(OptionalParamCodes.SarMsgRefNum, value);
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
				return GetOptionalParamByte(OptionalParamCodes.SarTotalSegments);
			}
			
			set
			{
				if(value == null || (value >= SarMin && value <= SarMax))
				{
					SetOptionalParamByte(OptionalParamCodes.SarTotalSegments, value);
				}
				else
				{
					throw new ArgumentException("sar_total_segments must be >= " + SarMin + " and <= " + SarMax);
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
				return GetOptionalParamByte(OptionalParamCodes.SarSegmentSeqnum);
			}
			
			set
			{
				if(value == null || (value >= SarMin && value <= SarMax))
				{
					SetOptionalParamByte(OptionalParamCodes.SarSegmentSeqnum, value);
				}
				else
				{
					throw new ArgumentException("sar_segment_seqnum must be >= " + SarMin + " and <= " + SarMax);
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
				return GetOptionalParamByte<PayloadTypeType>(OptionalParamCodes.PayloadType);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.PayloadType, value);
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
					OptionalParamCodes.MessagePayload);
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
				return GetOptionalParamByte<PrivacyType>(OptionalParamCodes.PrivacyIndicator);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.PrivacyIndicator, value);
			}
		}
		
		/// <summary>
		/// ESME assigned message reference number.
		/// </summary>
		public UInt16? UserMessageReference
		{
			get
			{				
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.UserMessageReference);
			}
			set
			{
				if(value == null || value < UInt16.MaxValue)
				{
					SetHostOrderValueIntoTlv(OptionalParamCodes.UserMessageReference, value);
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
				return GetOptionalParamByte(OptionalParamCodes.MsMsgWaitFacilities);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.MsMsgWaitFacilities, value);
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
				return GetOptionalParamByte<MsValidityType>(OptionalParamCodes.MsValidity);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.MsValidity, value);
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
				return GetHostOrderUInt16FromTlv(OptionalParamCodes.SmsSignal);
			}
			
			set
			{
				SetHostOrderValueIntoTlv(OptionalParamCodes.SmsSignal, value);
			}
		}
		
		/// <summary>
		/// The subcomponent in the destination device for which the user data is intended.
		/// </summary>
		public AddressSubunitType? DestinationAddrSubunit
		{
			get
			{
				return GetOptionalParamByte<AddressSubunitType>(OptionalParamCodes.DestAddrSubunit);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.DestAddrSubunit, value);
			}
		}
		
		/// <summary>
		/// Tells a mobile station to alert the user when the short message arrives at the MS.
		///
		/// Note: there is no value part associated with this parameter.
		/// Any value you pass in will be discarded.
		/// </summary>
		public byte? AlertOnMsgDelivery
		{
			get
			{
				try
				{
					byte[] bytes = GetOptionalParamBytes(
						OptionalParamCodes.AlertOnMessageDelivery);
					return 1;
				}
				catch(ApplicationException)
				{
					return 0;
				}
			}
			
			set
			{
				SetOptionalParamBytes(
					OptionalParamCodes.AlertOnMessageDelivery, new Byte[0]);
			}
		}
		
		/// <summary>
		/// The language of the short message.
		/// </summary>
		public LanguageIndicator? LanguageIndicator
		{
			get
			{
				return GetOptionalParamByte<LanguageIndicator>(OptionalParamCodes.LanguageIndicator);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.LanguageIndicator, value);
			}
		}
		
		/// <summary>
		/// Associates a display time with the short message on the MS.
		/// </summary>
		public DisplayTimeType? DisplayTime
		{
			get
			{
				return GetOptionalParamByte<DisplayTimeType>(OptionalParamCodes.DisplayTime);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.DisplayTime, value);
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
				return GetOptionalParamBytes(OptionalParamCodes.CallbackNum);
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
				return GetOptionalParamByte(OptionalParamCodes.CallbackNumPresInd);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.CallbackNumPresInd, value);
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
				return GetOptionalParamBytes(OptionalParamCodes.CallbackNumAtag);
			}
			
			set
			{
				const int callbackAtagMax = 65;

				if (value == null)
				{
					SetOptionalParamBytes(OptionalParamCodes.CallbackNumAtag, null);
				}
				else if (value.Length <= callbackAtagMax)
				{
					SetOptionalParamBytes(OptionalParamCodes.CallbackNumAtag, value);
				}
				else
				{
					throw new ArgumentException("Callback number atag must be <= " + callbackAtagMax + ".");
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
				return GetOptionalParamBytes(OptionalParamCodes.SourceSubaddress);
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
				return GetOptionalParamBytes(OptionalParamCodes.DestSubaddress);
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
