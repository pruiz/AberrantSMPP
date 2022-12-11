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
using System;
using AberrantSMPP.Packet;

namespace AberrantSMPP.Utility
{
	/// <summary>
	/// Marker/utility class to define destination addresses for the submit_multi.
	/// </summary>
	public sealed class DestinationAddress
	{
		private Pdu.TonType _DestinationAddressTon = Pdu.TonType.International;
		private Pdu.NpiType _DestinationAddressNpi = Pdu.NpiType.ISDN;
		private string _DestinationAddress = string.Empty;
		private string _DistributionList = string.Empty;
		private bool _IsDistributionList = false;
		
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
		public string DestAddress
		{
			get
			{
				return _DestinationAddress;
			}
		}
		
		/// <summary>
		/// Distribution list of destination address
		/// </summary>
		public string DistributionList
		{
			get
			{
				return _DistributionList;
			}
		}
		
		/// <summary>
		/// Set to true if this DestinationAddress is a DistributionList.
		/// </summary>
		public bool IsDistributionList
		{
			get
			{
				return _IsDistributionList;
			}
		}
		
		#endregion properties
		
		/// <summary>
		/// Creates an DestinationAddress address.  This will trim down the address given to
		/// it for use in future operations.
		/// </summary>
		/// <param name="address">The bytes of the response.</param>
		public DestinationAddress(ref byte[] address)
		{
			if(address[0] == 0x01)
			{
				_IsDistributionList = false;
			}
			else if(address[0] == 0x02)
			{
				_IsDistributionList = true;
			}
			else
			{
				throw new ApplicationException("Unable to determine type of destination address");
			}
			
			if(!IsDistributionList)
			{
				_DestinationAddressTon = (Pdu.TonType)address[1];
				_DestinationAddressNpi = (Pdu.NpiType)address[2];
				_DestinationAddress = SmppStringUtil.GetCStringFromBody(ref address, 3);
//				
//				long length = address.Length - 4;
//				byte[] newRemainder = new byte[length];
//				Array.Copy(address, 4, newRemainder, 0, length);
//				//and change the reference
//				address = newRemainder;
//				newRemainder = null;
			}
			else
			{
				_DistributionList = SmppStringUtil.GetCStringFromBody(ref address, 1);
//				
//				long length = address.Length - 4;
//				byte[] newRemainder = new byte[length];
//				Array.Copy(address, 4, newRemainder, 0, length);
//				//and change the reference
//				address = newRemainder;
//				newRemainder = null;
			}
		}
		
		/// <summary>
		/// Creates a new DestinationAddress.
		/// </summary>
		/// <param name="destinationAddressTon">Type of number for destination SME.</param>
		/// <param name="destinationAddressNpi">Numbering Plan Indicator for destination SME</param>
		/// <param name="destinationAdress">Destination Address of destination SME</param>
		public DestinationAddress(
			Pdu.TonType destinationAddressTon, 
			Pdu.NpiType destinationAddressNpi, 
			string destinationAdress)
		{
			if(destinationAdress == null || destinationAdress.Length > 20)
			{
				throw new ArgumentException(
					"Destination Adress must be 20 characters or less.");
			}
			
			_IsDistributionList = false;
			
			this._DestinationAddressTon = destinationAddressTon;
			this._DestinationAddressNpi = destinationAddressNpi;
			this._DestinationAddress = destinationAdress;
		}
		
		/// <summary>
		/// Creates a new DestinationAddress.
		/// </summary>
		/// <param name="distributionList">Distribution list of destination address</param>
		public DestinationAddress(string distributionList)
		{
			if(distributionList == null || distributionList.Length > 20)
			{
				throw new ArgumentException(
					"distribution list must be 20 characters or less.");
			}
			
			_IsDistributionList = true;
			
			this._DistributionList = distributionList;
		}
		
		/// <summary>
		/// Clones this DestinationAddress.
		/// </summary>
		/// <returns>The cloned object.</returns>
		public object Clone()
		{
			DestinationAddress temp = null;
			
			if(!this.IsDistributionList)
			{
				temp = new DestinationAddress(
					_DestinationAddressTon, _DestinationAddressNpi, _DestinationAddress);
			}
			else
			{
				temp = new DestinationAddress(_DistributionList);
			}
			
			temp._IsDistributionList = this.IsDistributionList;
			
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
	
	    DestinationAddress da = (DestinationAddress) obj;
	
	    if(!da.IsDistributionList)
	    {
	    	// value member check
		    return 
		    	_DestinationAddressTon.Equals(da._DestinationAddressTon) &&
		      _DestinationAddressNpi.Equals(da._DestinationAddressNpi) &&
		      _DestinationAddress.Equals(da._DestinationAddress);
	    }
	    else
	    {
	    	return _DistributionList.Equals(da._DistributionList);
	    }
		}
		
		/// <summary>
		/// Gets the hash code for this object.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
    {
			int hashCode = 0;
			
			if(!this.IsDistributionList)
			{
				hashCode ^= this.DestAddress.GetHashCode();
				hashCode ^= this.DestinationAddressNpi.GetHashCode();
				hashCode ^= this.DestinationAddressTon.GetHashCode();
			}
			else
			{
				hashCode ^= this.DistributionList.GetHashCode();
			}

			return hashCode;
    }
	}
}
