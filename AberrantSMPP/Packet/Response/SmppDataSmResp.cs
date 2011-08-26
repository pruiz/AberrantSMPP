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
using System.Collections;
using System.Text;
using AberrantSMPP.Utility;
using AberrantSMPP.Packet;

namespace AberrantSMPP.Packet.Response
{
	/// <summary>
	/// Response Pdu for the data_sm command.
	/// </summary>
	public class SmppDataSmResp : SmppSubmitSmResp
	{

		protected override CommandId DefaultCommandId { get { return CommandId.data_sm_resp; } }

		#region optional parameters
		
		/// <summary>
		/// Indicates the reason for delivery failure.
		/// </summary>
		public DeliveryFailureReason? DeliveryFailureReason
		{
			get
			{
				return GetOptionalParamByte<DeliveryFailureReason>(OptionalParamCodes.delivery_failure_reason);
			}
			
			set
			{
				if (value.HasValue)
				{
					SetOptionalParamBytes(OptionalParamCodes.delivery_failure_reason,
						BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering((byte)value)));
				}
				else
				{
					SetOptionalParamBytes(OptionalParamCodes.delivery_failure_reason, null);
				}
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
				return GetOptionalParamString(OptionalParamCodes.network_error_code);
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
				return GetOptionalParamString(OptionalParamCodes.additional_status_info_text);
			}
			
			set
			{
				const int MAX_STATUS_LEN = 264;

				if (value == null || value.Length <= MAX_STATUS_LEN)
				{
					SetOptionalParamString(OptionalParamCodes.additional_status_info_text, value);
				}
				else
				{
					throw new ArgumentException(
						"additional_status_info_text must have length <= " + MAX_STATUS_LEN);
				}
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
		
	}
}
