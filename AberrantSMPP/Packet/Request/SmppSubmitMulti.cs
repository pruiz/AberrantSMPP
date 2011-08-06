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
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Defines a submit_multi Pdu.
	/// </summary>
	public class SmppSubmitMulti : MessageLcd2
	{
		//note that all of the optional parameters are in the base class.
		private byte _NumberOfDests = 0;
		private DestinationAddress[] _DestinationAddresses = new DestinationAddress[0];
		private const int MAX_DESTS = 254;
		
		#region properties
		
		/// <summary>
		/// The number of destinations to send to.
		/// </summary>
		public byte NumberOfDestinations
		{
			get
			{
				return _NumberOfDests;
			}
		}
		
		/// <summary>
		/// The destination addresses that will be sent to.  This will set the number of 
		/// destination addresses as well.  Passing null in will set them to zero.  Calling the 
		/// accessor will get you a cloned array.  You must use the mutator to modify the values.
		/// </summary>
		public DestinationAddress[] DestinationAddresses
		{
			get
			{
				return(DestinationAddress[])_DestinationAddresses.Clone();
			}
			set
			{
				_DestinationAddresses = (value == null) ? new DestinationAddress[0] : value;
				_NumberOfDests = (byte)_DestinationAddresses.Length;
			}
		}
		
		#endregion properties
		
		#region constructors
		
		/// <summary>
		/// Creates a submit_multi Pdu.  Sets source address TON to international, 
		/// source address NPI to ISDN, source address to "", registered delivery type to none, 
		/// ESM class to 0, data coding to SMSC default, protocol ID to v3.4, priority to level 1,
		/// validity period to default, replace if present to false, default message ID to 0, 
		/// the short message to an empty string, the number of destinations to 0 and the 
		/// destination addresses to null.  Use the DestAddrFactory class to create 
		/// destination addresses.
		/// </summary>
		public SmppSubmitMulti(): base()
		{}
		
		/// <summary>
		/// Creates a submit_multi Pdu.  Sets source address TON to international, 
		/// source address NPI to ISDN, source address to "", registered delivery type to none, 
		/// ESM class to 0, data coding to SMSC default, protocol ID to v3.4, priority to level 1,
		/// validity period to default, replace if present to false, default message ID to 0, 
		/// the short message to an empty string, the number of destinations to 0 and the 
		/// destination addresses to null.  Use the DestAddrFactory class to create 
		/// destination addresses.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes from an ESME</param>
		public SmppSubmitMulti(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
		
		/// <summary>
		/// Initializes this Pdu.
		/// </summary>
		protected override void InitPdu()
		{
			base.InitPdu();
			CommandStatus = 0;
			CommandID = CommandIdType.submit_multi;
		}
		
		///<summary>
		/// Gets the hex encoding(big-endian)of this Pdu.
		///</summary>
		///<return>The hex-encoded version of the Pdu</return>
		public override void ToMsbHexEncoding()
		{
			ArrayList pdu = GetPduHeader();
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(ServiceType)));
			pdu.Add((byte)SourceAddressTon);
			pdu.Add((byte)SourceAddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SourceAddress)));
			//number of destinations.
			pdu.Add(NumberOfDestinations);
			
			//and their addresses
			foreach(DestinationAddress address in _DestinationAddresses)
			{
				if(!address.IsDistributionList)
				{
					//pack up the byte array for this address
					ArrayList sme = new ArrayList();
					pdu.Add((byte)0x01);
					pdu.Add((byte)address.DestinationAddressTon);
					pdu.Add((byte)address.DestinationAddressNpi);
					pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(
						Encoding.ASCII.GetBytes(address.DestAddress)));
				}
				else
				{
					ArrayList dln = new ArrayList();
					pdu.Add((byte)0x02);
					pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(
						Encoding.ASCII.GetBytes(address.DistributionList)));
				}
			}
			
			pdu.AddRange(base.GetBytesAfterDestination());
			
			PacketBytes = EncodePduForTransmission(pdu);
		}
		
		/// <summary>
		/// Decodes the submit_multi response from the SMSC.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			
			ServiceType = SmppStringUtil.GetCStringFromBody(ref remainder);
			SourceAddressTon =(TonType)remainder[0];
			SourceAddressNpi =(NpiType)remainder[1];
			SourceAddress = SmppStringUtil.GetCStringFromBody(ref remainder, 2);
			
			//the SMSC might not send back the number of destinations,
			//so check if it did
			if(remainder.Length > 0)
			{
				_NumberOfDests = remainder[0];
				DestinationAddresses = new DestinationAddress[NumberOfDestinations];
				
				//trim off the number of destinations
				long length = remainder.Length - 1;
				byte[] newRemainder = new byte[length];
				Array.Copy(remainder, 1, newRemainder, 0, length);
				remainder = newRemainder;
				newRemainder = null;
				
				for(int i = 0; i < _DestinationAddresses.Length; i++)
				{
					_DestinationAddresses[i] = new DestinationAddress(ref remainder);
				}
			}
			
			EsmClass = remainder[0];
			ProtocolId =(SmppVersionType)remainder[1];
			PriorityFlag =(PriorityType)remainder[2];
			ScheduleDeliveryTime = SmppStringUtil.GetCStringFromBody(ref remainder, 3);
			ValidityPeriod = SmppStringUtil.GetCStringFromBody(ref remainder);
			RegisteredDelivery =(RegisteredDeliveryType)remainder[0];
			ReplaceIfPresentFlag =(remainder[1] == 0)? false : true;
			DataCoding =(DataCoding)remainder[2];
			SmDefaultMessageId = remainder[3];
			_SmLength = remainder[4];
			ShortMessage = SmppStringUtil.GetStringFromBody(ref remainder, 5, 5 + _SmLength);
			
			//fill the TLV table if applicable
			TranslateTlvDataIntoTable(remainder);
		}
	}
}
