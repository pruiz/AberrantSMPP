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
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections;
using System.Text;
using RoaminSMPP.Utility;
using RoaminSMPP.Packet;

namespace RoaminSMPP.Packet.Response
{
	/// <summary>
	/// Response Pdu for the data_sm command.
	/// </summary>
	public class SmppDataSmResp : SmppSubmitSmResp
	{
		/// <summary>
		/// Enumerates the delivery failure types.
		/// </summary>
		public enum DeliveryFailureType : byte
		{
			/// <summary>
			/// DestinationUnavailable
			/// </summary>
			DestinationUnavailable = 0x00,
			/// <summary>
			/// DestinationAddressInvalid
			/// </summary>
			DestinationAddressInvalid = 0x01,
			/// <summary>
			/// PermanentNetworkError
			/// </summary>
			PermanentNetworkError = 0x02,
			/// <summary>
			/// TemporaryNetworkError
			/// </summary>
			TemporaryNetworkError = 0x03
		}
		
		#region optional parameters
		
		/// <summary>
		/// Indicates the reason for delivery failure.
		/// </summary>
		public DeliveryFailureType DeliveryFailureReason
		{
			get
			{
				return(DeliveryFailureType)
					GetOptionalParamBytes((ushort)
					Pdu.OptionalParamCodes.delivery_failure_reason)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(UInt16)Pdu.OptionalParamCodes.delivery_failure_reason,
					BitConverter.GetBytes(
					UnsignedNumConverter.SwapByteOrdering((byte)value)));
			}
		}
		
		/// <summary>
		/// Error code specific to a wireless network.  See SMPP spec section
		/// 5.3.2.31 for details.
		/// </summary>
		public string NetworkErrorCode
		{
			get
			{
				return GetOptionalParamString((ushort)
					Pdu.OptionalParamCodes.network_error_code);
			}
			
			set
			{
				PduUtil.SetNetworkErrorCode(this, value);
			}
		}
		
		/// <summary>
		/// Text(ASCII)giving additional info on the meaning of the response.
		/// </summary>
		public string AdditionalStatusInfoText
		{
			get
			{
				return GetOptionalParamString((ushort)
					Pdu.OptionalParamCodes.additional_status_info_text);
			}
			
			set
			{
				const int MAX_STATUS_LEN = 264;
				if(value == null)
				{
					SetOptionalParamString(
						(ushort)Pdu.OptionalParamCodes.additional_status_info_text, string.Empty);
				}
				else if(value.Length <= MAX_STATUS_LEN)
				{
					SetOptionalParamString(
						(ushort)Pdu.OptionalParamCodes.additional_status_info_text, value);
				}
				else
				{
					throw new ArgumentException(
						"additional_status_info_text must have length <= " + MAX_STATUS_LEN);
				}
			}
		}
		
		/// <summary>
		/// Indicates whether the Delivery Pending Flag was set.
		/// </summary>
		public DpfResultType DpfResult
		{
			get
			{
				return(DpfResultType)
					GetOptionalParamBytes((ushort)
					Pdu.OptionalParamCodes.dpf_result)[0];
			}
			
			set
			{
				SetOptionalParamBytes(
					(UInt16)Pdu.OptionalParamCodes.dpf_result,
					BitConverter.GetBytes(
					UnsignedNumConverter.SwapByteOrdering((byte)value)));
			}
		}
		
		#endregion optional parameters
		
		#region constructors
		
		/// <summary>
		/// Creates a data_sm_resp Pdu.
		/// </summary>
		public SmppDataSmResp(): base()
		{}
		
		/// <summary>
		/// Creates a data_sm_resp Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppDataSmResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Initializes this Pdu for sending purposes.
		/// </summary>
		protected override void InitPdu()
		{
			base.InitPdu();
			CommandStatus = 0;
			CommandID = CommandIdType.data_sm_resp;
		}
		
		///<summary>
		/// Gets the hex encoding(big-endian)of this Pdu.
		///</summary>
		///<return>The hex-encoded version of the Pdu</return>
		public override void ToMsbHexEncoding()
		{
			ArrayList pdu = GetPduHeader();
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(MessageId)));
			
			PacketBytes = EncodePduForTransmission(pdu);
		}
	}
}
