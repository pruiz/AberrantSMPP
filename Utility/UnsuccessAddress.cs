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
using RoaminSMPP.Packet;

namespace RoaminSMPP.Utility
{
	/// <summary>
	/// Defines an Unsuccess address, used with the response to submit_multi.
	/// The spec states that both SME addresses and distribution lists can be
	/// used here, but it only defines for the SME address, so that is all that
	/// this will handle.
	/// </summary>
	public class UnsuccessAddress
	{
		private Pdu.TonType _DestinationAddressTon;
		private Pdu.NpiType _DestinationAddressNpi;
		private string _DestinationAddress;
		private uint _ErrorStatusCode;
		
		#region properties
		
		/// <summary>
		/// Type of number for destination SME.
		/// </summary>
		public Pdu.TonType DestinationAddressTon
		{
			get
			{
				return _DestinationAddressTon;
			}
		}
		
		/// <summary>
		/// Numbering Plan Indicator for destination SME
		/// </summary>
		public Pdu.NpiType DestinationAddressNpi
		{
			get
			{
				return _DestinationAddressNpi;
			}
		}
		
		/// <summary>
		/// Destination Address of destination SME
		/// </summary>
		public string DestinationAddress
		{
			get
			{
				return _DestinationAddress;
			}
		}
		
		/// <summary>
		/// Indicates the success or failure of the submit_multi request to this
		/// SME address.
		/// </summary>
		public UInt32 ErrorStatusCode
		{
			get
			{
				return _ErrorStatusCode;
			}
		}
		
		#endregion properties
		
		/// <summary>
		/// Creates an Unsuccess address.  This will trim down the address given to
		/// it for use in future operations.
		/// </summary>
		/// <param name="address">The bytes of the response.</param>
		public UnsuccessAddress(ref byte[] address)
		{
			_DestinationAddressTon = (Pdu.TonType)address[0];
			_DestinationAddressNpi = (Pdu.NpiType)address[1];
			_DestinationAddress = SmppStringUtil.GetCStringFromBody(ref address, 2);
			//convert error status to host order
			_ErrorStatusCode = UnsignedNumConverter.SwapByteOrdering(
									 BitConverter.ToUInt32(address, 0));
			//now we have to trim off four octets to account for the status code
			long length = address.Length - 4;
			byte[] newRemainder = new byte[length];
			Array.Copy(address, 4, newRemainder, 0, length);
			//and change the reference
			address = newRemainder;
			newRemainder = null;
		}
		
		/// <summary>
		/// Creates a new UnsuccessAdress.
		/// </summary>
		/// <param name="destinationAddressTon">Type of number for destination SME.</param>
		/// <param name="destinationAddressNpi">Numbering Plan Indicator for destination SME</param>
		/// <param name="destinationAdress">Destination Address of destination SME</param>
		/// <param name="ErrorStatusCode">Indicates the success or failure of the submit_multi request 
		/// to this SME address.</param>
		public UnsuccessAddress(
			Pdu.TonType destinationAddressTon, 
			Pdu.NpiType destinationAddressNpi, 
			string destinationAdress, 
			UInt32 ErrorStatusCode)
		{
			this._DestinationAddressTon = destinationAddressTon;
			this._DestinationAddressNpi = destinationAddressNpi;
			this._DestinationAddress = destinationAdress;
			this._ErrorStatusCode = ErrorStatusCode;
		}
		
		/// <summary>
		/// Clones this UnsuccessAddress.
		/// </summary>
		/// <returns>The cloned object.</returns>
		public object Clone()
		{
			UnsuccessAddress temp = new UnsuccessAddress(
				_DestinationAddressTon, _DestinationAddressNpi, _DestinationAddress, _ErrorStatusCode);
			return temp;
		}
		
		/// <summary>
		/// Checks to see if two UnsuccessAddresses are equal.
		/// </summary>
		/// <param name="obj">The UnsuccessAddresses to check</param>
		/// <returns>true if obj and this are equal</returns>
		public override bool Equals(object obj)
		{
	    if (obj == null)
	    {
	    	return false;
	    }
	
	    if (this.GetType() != obj.GetType())
	    {
	    	return false;
	    }
	
	    // safe because of the GetType check
	    UnsuccessAddress us = (UnsuccessAddress) obj;
	
	    // value member check
	    return 
	    	_DestinationAddressTon.Equals(us._DestinationAddressTon) &&
	      _DestinationAddressNpi.Equals(us._DestinationAddressNpi) &&
	      _DestinationAddress.Equals(us._DestinationAddress) &&
	     	_ErrorStatusCode.Equals(us._ErrorStatusCode);
		}
		
		/// <summary>
		/// Gets the hash code for this object.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
    {
			int hashCode = 0;
			hashCode ^= this.DestinationAddress.GetHashCode();
			hashCode ^= this.DestinationAddressNpi.GetHashCode();
			hashCode ^= this.DestinationAddressTon.GetHashCode();
			hashCode ^= this.ErrorStatusCode.GetHashCode();

			return hashCode;
    }
	}
}
