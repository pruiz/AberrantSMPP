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
using AberrantSMPP.Packet;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Provides some common attributes for data_sm, query_sm, submit_sm, submit_multi,
	/// cancel_sm, and replace_sm.
	/// </summary>
	public abstract class MessageLcd6 : Pdu
	{
		#region private fields
		
		private TonType _SourceAddressTon = Pdu.TonType.International;
		private NpiType _SourceAddressNpi = Pdu.NpiType.ISDN;
		private string _SourceAddress = string.Empty;
		
		#endregion private fields
		
		#region constants
		/// <summary>
		/// Address length for source and destination addresses
		/// </summary>
		protected const int ADDRESS_LENGTH = 20;
		/// <summary>
		/// Message length
		/// </summary>
		protected const int MSG_LENGTH = 64;
		/// <summary>
		/// Length of service type string
		/// </summary>
		protected const int SERVICE_TYPE_LENGTH = 50;
		
		#endregion constants
		
		#region properties
		
		/// <summary>
		/// The type of number of the source address.
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
		/// The number plan indicator of the source address.
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
		/// The source address.  Null values are treated as empty strings.
		/// </summary>
		public string SourceAddress
		{
			get
			{
				return _SourceAddress;
			}
			set
			{
				if(value != null)
				{
					if(value.Length <= ADDRESS_LENGTH)
					{
						_SourceAddress = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException(
							"Source Address must be <= " + ADDRESS_LENGTH + " characters.");
					}
				}
				else
				{
					_SourceAddress = string.Empty;
				}
			}
		}
		
		#endregion properties
		
		#region constructors
		
		/// <summary>
		/// Groups construction tasks for subclasses.  Sets source address TON to 
		/// international, source address NPI to ISDN, and source address to "".
		/// </summary>
		protected MessageLcd6(): base()
		{}
		
		/// <summary>
		/// Creates a new MessageLcd6 for incoming PDUs.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to decode.</param>
		protected MessageLcd6(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
	}
}
