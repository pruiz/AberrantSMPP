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

using System.Text;
using System.Collections;
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
		
		private TonType _sourceAddressTon = TonType.International;
		private NpiType _sourceAddressNpi = NpiType.Isdn;
		private string _sourceAddress = string.Empty;
		private TonType _esmeAddressTon = TonType.International;
		private NpiType _esmeAddressNpi = NpiType.Isdn;
		private string _esmeAddress = string.Empty;
		
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
		protected override CommandId DefaultCommandId { get { return CommandId.AlertNotification; } }

		/// <summary>
		/// Enumerates the type of number.
		/// </summary>
		public TonType SourceAddressTon
		{
			get
			{
				return _sourceAddressTon;
			}
			
			set
			{
				_sourceAddressTon = value;
			}
		}
		
		/// <summary>
		/// Enumerates the numbering plan indicator.
		/// </summary>
		public NpiType SourceAddressNpi
		{
			get
			{
				return _sourceAddressNpi;
			}
			
			set
			{
				_sourceAddressNpi = value;
			}
		}
		
		/// <summary>
		/// Address of sending entity.
		/// </summary>
		public string SourceAddress
		{
			get
			{
				return _sourceAddress;
			}
			
			set
			{
				_sourceAddress = (value == null) ? string.Empty : value;
			}
		}
		
		/// <summary>
		/// The type of number for the destination address that requested an alert.
		/// </summary>
		public TonType EsmeAddressTon
		{
			get
			{
				return _esmeAddressTon;
			}
			
			set
			{
				_esmeAddressTon = value;
			}
		}
		
		/// <summary>
		/// The numbering plan indicator for the destination address that requested an alert.
		/// </summary>
		public NpiType EsmeAddressNpi
		{
			get
			{
				return _esmeAddressNpi;
			}
			
			set
			{
				_esmeAddressNpi = value;
			}
		}
		
		/// <summary>
		/// The source address of the device that requested an alert.
		/// </summary>
		public string EsmeAddress
		{
			get
			{
				return _esmeAddress;
			}
			
			set
			{				
				_esmeAddress = (value == null) ? string.Empty : value;
			}
		}
		
		#endregion mandatory parameters
		
		#region optional parameters
		
		/// <summary>
		/// The status of the mobile station.
		/// </summary>
		public AvailabilityStatusType? MsAvailabilityStatus
		{
			get
			{
				return GetOptionalParamByte<AvailabilityStatusType>(OptionalParamCodes.MsAvailabilityStatus);
			}
			
			set
			{
				SetOptionalParamByte(OptionalParamCodes.MsAvailabilityStatus, value);
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
