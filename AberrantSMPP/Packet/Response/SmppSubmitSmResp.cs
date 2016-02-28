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
using System.Collections;
using System.Text;
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Response
{
	/// <summary>
	/// Defines the submit_sm response from the SMSC.
	/// </summary>
	public class SmppSubmitSmResp : SmppResponse
	{
		private string _messageId = string.Empty;
		private byte[] _responseAfterMsgId;
		
		protected override CommandId DefaultCommandId { get { return CommandId.SubmitSmResp; } }

		/// <summary>
		/// The message ID(SMSC-assigned)of the submitted message.
		/// </summary>
		public string MessageId
		{
			get
			{
				return _messageId;
			}
			
			set
			{
				_messageId = (value == null) ? string.Empty : value;
			}
		}

		/// <summary>
		/// Indicates whether the Delivery Pending Flag was set.
		/// </summary>
		public DpfResultType? DpfResult
		{
			get
			{
				return GetOptionalParamByte<DpfResultType>(OptionalParamCodes.DpfResult);
			}

			set
			{
				if (value.HasValue)
				{
					SetOptionalParamBytes(OptionalParamCodes.DpfResult,
						BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering((byte)value)));
				}
				else
				{
					SetOptionalParamBytes(OptionalParamCodes.DpfResult, null);
				}
			}
		}

		/// <summary>
		/// Accessor for the submit_multi to get at the response after the message ID.
		/// This is, in essence, set only after DecodeSmscResponse()in this base class
		/// is called.
		/// </summary>
		protected byte[] ResponseAfterMsgId
		{
			get
			{
				return _responseAfterMsgId;
			}
		}
		
		#region constructors
		
		/// <summary>
		/// Creates a submit_sm response Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppSubmitSmResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates a submit_sm_resp
		/// </summary>
		public SmppSubmitSmResp(): base()
		{
		}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the submit response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			DecodeNonTlv();
			//fill the TLV table if applicable
			TranslateTlvDataIntoTable(_responseAfterMsgId);
		}
		
		/// <summary>
		/// Decodes the non-TLV bytes.  Needed for the submit_multi_resp.
		/// </summary>
		protected void DecodeNonTlv()
		{
			//header
			byte[] remainder = BytesAfterHeader;
			MessageId = SmppStringUtil.GetCStringFromBody(ref remainder);
			_responseAfterMsgId = remainder;
		}
		
		protected override void AppendPduData(ArrayList pdu)
		{
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(MessageId)));
		}
	}
}
