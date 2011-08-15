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
	/// Sent from the SMSC to the mobile device when the device is available and a
	/// delivery pending flag has been set from a previous data_sm operation.
	/// </summary>
	public class SmppAlertNotification : SmppRequest
	{
		#region private fields
		
		private TonType _SourceAddressTon = TonType.International;
		private NpiType _SourceAddressNpi = NpiType.ISDN;
		private string _SourceAddress = string.Empty;
		private TonType _EsmeAddressTon = TonType.International;
		private NpiType _EsmeAddressNpi = NpiType.ISDN;
		private string _EsmeAddress = string.Empty;
		
		#endregion private fields

		#region enumerations
		
		/// <summary>
		/// Enumerates the availability states of the message.
		/// </summary>
		public enum AvailabilityStatusType : byte
		{
			/// <summary>
			/// Available
			/// </summary>
			Available = 0x00,
			/// <summary>
			/// Denied
			/// </summary>
			Denied = 0x01,
			/// <summary>
			/// Unavailable
			/// </summary>
			Unavailable = 0x02
		}
		
		#endregion enumerations
		
		#region mandatory parameters
		protected override CommandId DefaultCommandId { get { return CommandId.alert_notification; } }

		/// <summary>
		/// Enumerates the type of number.
		/// </summary>
		public TonType SourceAddressTon
		{
			get
			{
				return _SourceAddressTon;
			}
			
			set
			{
				_SourceAddressTon = value;
			}
		}
		
		/// <summary>
		/// Enumerates the numbering plan indicator.
		/// </summary>
		public NpiType SourceAddressNpi
		{
			get
			{
				return _SourceAddressNpi;
			}
			
			set
			{
				_SourceAddressNpi = value;
			}
		}
		
		/// <summary>
		/// Address of sending entity.
		/// </summary>
		public string SourceAddress
		{
			get
			{
				return _SourceAddress;
			}
			
			set
			{
				_SourceAddress = (value == null) ? string.Empty : value;
			}
		}
		
		/// <summary>
		/// The type of number for the destination address that requested an alert.
		/// </summary>
		public TonType EsmeAddressTon
		{
			get
			{
				return _EsmeAddressTon;
			}
			
			set
			{
				_EsmeAddressTon = value;
			}
		}
		
		/// <summary>
		/// The numbering plan indicator for the destination address that requested an alert.
		/// </summary>
		public NpiType EsmeAddressNpi
		{
			get
			{
				return _EsmeAddressNpi;
			}
			
			set
			{
				_EsmeAddressNpi = value;
			}
		}
		
		/// <summary>
		/// The source address of the device that requested an alert.
		/// </summary>
		public string EsmeAddress
		{
			get
			{
				return _EsmeAddress;
			}
			
			set
			{				
				_EsmeAddress = (value == null) ? string.Empty : value;
			}
		}
		
		#endregion mandatory parameters
		
		#region optional parameters
		
		/// <summary>
		/// The status of the mobile station.
		/// </summary>
		public AvailabilityStatusType? MSAvailabilityStatus
		{
			get
			{
				return GetOptionalParamByte<AvailabilityStatusType>(Pdu.OptionalParamCodes.ms_availability_status);
			}
			
			set
			{
				SetOptionalParamByte(Pdu.OptionalParamCodes.ms_availability_status, value);
			}
		}
		
		#endregion optional parameters
		
		#region constructors
		
		/// <summary>
		/// Creates an SMPP Alert Notification Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppAlertNotification(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates an SMPP Alert Notification Pdu.
		/// </summary>
		public SmppAlertNotification(): base()
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the alert_notification from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			SourceAddressTon =(TonType)remainder[0];
			SourceAddressNpi =(NpiType)remainder[1];
			SourceAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			EsmeAddressTon =(TonType)remainder[0];
			EsmeAddressNpi =(NpiType)remainder[1];
			EsmeAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			//fill the TLV table if applicable
			TranslateTlvDataIntoTable(remainder);
		}
		
		protected override void AppendPduData(ArrayList pdu)
		{
			pdu.Add((byte)SourceAddressTon);
			pdu.Add((byte)SourceAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SourceAddress)));
			pdu.Add((byte)EsmeAddressTon);
			pdu.Add((byte)EsmeAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(EsmeAddress)));
		}
	}
}
