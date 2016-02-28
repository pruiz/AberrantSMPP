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
	/// Defines an SMPP submit request.
	/// </summary>
	public class SmppSubmitSm : SmppSubmit
	{
		#region private fields

		private TonType _destinationAddressTon;
		private NpiType _destinationAddressNpi;
		private string _destinationAddress;
		
		#endregion private fields
		
		#region mandatory parameters
		protected override CommandId DefaultCommandId { get { return CommandId.SubmitSm; } }

		/// <summary>
		/// Type of Number for destination.
		/// </summary>
		public TonType DestinationAddressTon
		{
			get
			{
				return _destinationAddressTon;
			}
			set
			{
				_destinationAddressTon = value;
			}
		}
		
		/// <summary>
		/// Numbering Plan Indicator for destination.
		/// </summary>
		public NpiType DestinationAddressNpi
		{
			get
			{
				return _destinationAddressNpi;
			}
			set
			{
				_destinationAddressNpi = value;
			}
		}
		
		/// <summary>
		/// Destination address of this short message.  Null values will be converted to an
		/// empty string.
		/// </summary>
		public string DestinationAddress
		{
			get
			{
				return _destinationAddress;
			}
			set
			{
				if(value != null)
				{
					if(value.ToString().Length <= AddressLength)
					{
						_destinationAddress = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException(
							"Destination Address too long(must be <= "+ AddressLength + " 20 characters).");
					}
				}
				else
				{
					_destinationAddress = string.Empty;
				}
			}
		}
		
		#endregion mandatory parameters
		
		#region optional params
		
		/// <summary>
		/// Indicates to the SMSC that there are further messages for the same destination.
		/// </summary>
		public bool? MoreMessagesToSend
		{
			get
			{
				return GetOptionalParamByte<bool>(OptionalParamCodes.MoreMessagesToSend);
			}
			
			set
			{
				//byte? data = value.HasValue ? new Nullable<byte>(Convert.ToByte(value.Value)) : null;
				SetOptionalParamByte(OptionalParamCodes.MoreMessagesToSend, value);
			}
		}
		
		/// <summary>
		/// A response code set by the user in a User Acknowledgement/Reply message. The
		/// response codes are application specific.  From the SMPP spec:
		///
		/// 0 to 255(IS-95 CDMA)
		/// 0 to 15(CMT-136 TDMA)
		/// </summary>
		public byte? UserResponseCode
		{
			get
			{
				return GetOptionalParamByte(OptionalParamCodes.UserResponseCode);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.UserResponseCode, value);
			}
		}
		
		/// <summary>
		/// Used to indicate the number of messages stored in a mailbox.
		/// </summary>
		public byte? NumberOfMessages
		{
			get
			{
				return GetOptionalParamByte(OptionalParamCodes.NumberOfMessages);
			}
			
			set
			{
				const int maxNumMsgs = 99;

				if(value == null || value <= maxNumMsgs)
				{
					SetOptionalParamByte(OptionalParamCodes.NumberOfMessages, value);
				}
				else
				{
					throw new ArgumentException(	"number_of_messages must be between 0 and " + maxNumMsgs + ".");
				}
			}
		}
		
		/// <summary>
		/// From the SMPP spec:
		/// The its_reply_type parameter is a required parameter for the CDMA Interactive
		/// Teleservice as defined by the Korean PCS carriers [KORITS]. It indicates and
		/// controls the MS users reply method to an SMS delivery message received from
		/// the ESME.
		/// </summary>
		public ItsReplyTypeType? ItsReplyType
		{
			get
			{
				return GetOptionalParamByte<ItsReplyTypeType>(OptionalParamCodes.ItsReplyType);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.ItsReplyType, value);
			}
		}
		
		/// <summary>
		/// From the SMPP spec:
		/// The its_session_info parameter is a required parameter for the CDMA Interactive
		/// Teleservice as defined by the Korean PCS carriers [KORITS]. It contains control
		/// information for the interactive session between an MS and an ESME.
		///
		/// See section 5.3.2.43 of the SMPP spec for how to set this.
		/// </summary>
		public byte[] ItsSessionInfo
		{
			get
			{
				return GetOptionalParamBytes(OptionalParamCodes.ItsSessionInfo);
			}
			
			set
			{
				PduUtil.SetItsSessionInfo(this, value);
			}
		}
		
		/// <summary>
		/// From the SMPP spec:
		/// The ussd_service_op parameter is required to define the USSD service operation
		/// when SMPP is being used as an interface to a(GSM)USSD system.
		///
		/// See 5.3.2.44 of the SMPP spec for how to set this.
		/// </summary>
		public byte? UssdServiceOp
		{
			get
			{
				return GetOptionalParamByte(OptionalParamCodes.UssdServiceOp);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.UssdServiceOp, value);
			}
		}
		
		#endregion optional params
		
		#region constructors
		
		/// <summary>
		/// Creates a submit Pdu.  Sets source address TON to international, 
		/// source address NPI to ISDN, source address to "", registered delivery type to none, 
		/// ESM class to 0, data coding to SMSC default, default protocol-id, priority to level 1,
		/// validity period to default, replace if present to false, default message ID to 0, 
		/// the short message to an empty string, the destination address TON to international, 
		/// destination address NPI to ISDN, the destination address to "", the command status 
		/// to 0, and the Command ID to submit_sm.
		/// </summary>
		public SmppSubmitSm(): base()
		{
			DestinationAddressTon = Pdu.TonType.International;
			DestinationAddressNpi = Pdu.NpiType.Isdn;
			DestinationAddress = null;
			CommandStatus = 0;
		}
		
		/// <summary>
		/// Creates a new SmppSubmitSm for incoming PDUs.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to decode.</param>
		public SmppSubmitSm(byte[] incomingBytes): base(incomingBytes)
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
			pdu.AddRange(GetBytesAfterDestination());
		}
		
		/// <summary>
		/// This decodes the submit_sm Pdu.  The Pdu has basically the same format as
		/// the submit_sm Pdu, but in this case it is a response.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			ServiceType = SmppStringUtil.GetCStringFromBody(ref remainder);
			SourceAddressTon =(TonType)remainder[0];
			SourceAddressNpi =(NpiType)remainder[1];
			SourceAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			DestinationAddressTon =(TonType)remainder[0];
			DestinationAddressNpi =(NpiType)remainder[1];
			DestinationAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			EsmClass = remainder[0];
			ProtocolId = remainder[1];
			PriorityFlag =(PriorityType)remainder[2];
			ScheduleDeliveryTime = SmppStringUtil.GetCStringFromBody(ref remainder, 3);
			ValidityPeriod = SmppStringUtil.GetCStringFromBody(ref remainder);
			RegisteredDelivery =(RegisteredDeliveryType)remainder[0];
			ReplaceIfPresentFlag =(remainder[1] == 0)? false : true;
			DataCoding =(DataCoding)remainder[2];
			SmDefaultMessageId = remainder[3];
			_SmLength = remainder[4];
			ShortMessage = SmppStringUtil.GetStringFromBody(ref remainder, 5, 5 + _SmLength);
			
			TranslateTlvDataIntoTable(remainder);
		}
	}
}
