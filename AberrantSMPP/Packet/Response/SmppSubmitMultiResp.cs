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
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Response
{
	/// <summary>
	/// Description of SmppSubmitMultiResp.
	/// </summary>
	public class SmppSubmitMultiResp : SmppSubmitSmResp
	{
		private byte _NumberUnsuccessful = 0;
		private UnsuccessAddress[] _UnsuccessfulAddresses = new UnsuccessAddress[0];
		
		/// <summary>
		/// The number of messages that could not be delivered.  The mutator is not 
		/// provided as setting the unsucess addresses will set it.
		/// </summary>
		public byte NumberUnsuccessful
		{
			get
			{
				return _NumberUnsuccessful;
			}
		}
		
		/// <summary>
		/// The array of unsuccessful addresses.  This will set the number of addresses as well.  
		/// Passing in null will set them to zero.  Calling the accessor will get you a cloned array.  
		/// You must use the set accessor to modify the values.
		/// </summary>
		public UnsuccessAddress[] UnsuccessfulAddresses
		{
			get
			{
				return(UnsuccessAddress[])_UnsuccessfulAddresses.Clone();
			}
			
			set
			{
				_UnsuccessfulAddresses = (value == null) ? new UnsuccessAddress[0] : value;
				_NumberUnsuccessful = (byte)_UnsuccessfulAddresses.Length;
			}
		}
		
		#region constructors
		
		/// <summary>
		/// Creates a submit_multi response Pdu.
		/// </summary>
		/// <param name="incomingBytes">The bytes received from an ESME.</param>
		public SmppSubmitMultiResp(byte[] incomingBytes): base(incomingBytes)
		{}
		
		/// <summary>
		/// Creates a submit_multi response Pdu.
		/// </summary>
		public SmppSubmitMultiResp(): base()
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Decodes the submit_multi response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			DecodeNonTlv();
			
			byte[] remainder = base.ResponseAfterMsgId;
			//the SMSC might not send back the number of unsuccessful messages,
			//so check if it did
			if(remainder.Length > 0)
			{
				_NumberUnsuccessful = remainder[0];
				UnsuccessfulAddresses = new UnsuccessAddress[NumberUnsuccessful];
				long length = remainder.Length - 1;
				byte[] newRemainder = new byte[length];
				Array.Copy(remainder, 1, newRemainder, 0, length);
				remainder = newRemainder;
				newRemainder = null;
				//unsuccessful
				for(int i = 0; i < UnsuccessfulAddresses.Length; i++)
				{
					_UnsuccessfulAddresses[i] = new UnsuccessAddress(ref remainder);
				}
			}
			
			//fill the TLV table if applicable
			TranslateTlvDataIntoTable(remainder);
		}
		
		/// <summary>
		/// Initializes this Pdu.
		/// </summary>
		protected override void InitPdu()
		{
			base.InitPdu();
			CommandStatus = 0;
			CommandID = CommandIdType.submit_multi_resp;
		}
		
		/// <summary>
		/// Creates the byte encoding for this Pdu.
		/// </summary>
		public override void ToMsbHexEncoding()
		{
			ArrayList pdu = GetPduHeader();
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(MessageId)));
			pdu.Add(NumberUnsuccessful);
			//add the unsuccess addresses
			UnsuccessAddress[] unsuccessfulAddresses = UnsuccessfulAddresses;
			
			for(int i = 0; i < NumberUnsuccessful; i++)
			{
				pdu.Add((byte)unsuccessfulAddresses[i].DestinationAddressTon);
				pdu.Add((byte)unsuccessfulAddresses[i].DestinationAddressNpi);
				pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(
					Encoding.ASCII.GetBytes(unsuccessfulAddresses[i].DestinationAddress)));
				pdu.AddRange(BitConverter.GetBytes(
					UnsignedNumConverter.SwapByteOrdering(unsuccessfulAddresses[i].ErrorStatusCode)));
			}
//
			PacketBytes = EncodePduForTransmission(pdu);
		}
	}
}
