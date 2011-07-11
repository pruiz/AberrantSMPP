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
using RoaminSMPP.Packet;

namespace RoaminSMPP.Packet.Request
{
	/// <summary>
	/// Provides some common attributes for data_sm, submit_sm, submit_multi,
	/// and replace_sm.
	/// </summary>
	public abstract class MessageLcd4 : MessageLcd6
	{
		/// <summary>
		/// The registered delivery type of the message.
		/// </summary>
		protected RegisteredDeliveryType _RegisteredDelivery = Pdu.RegisteredDeliveryType.None;
		
		#region properties
		
		/// <summary>
		/// The registered delivery type of the message.
		/// </summary>
		public RegisteredDeliveryType RegisteredDelivery
		{
			get
			{
				return _RegisteredDelivery;
			}
			set
			{
				_RegisteredDelivery =  value;
			}
		}
		
		#endregion properties
		
		#region constructors
		
		/// <summary>
		/// Groups construction tasks for subclasses.  Sets source address TON to international, 
		/// source address NPI to ISDN, source address to "", and registered delivery type to 
		/// none.
		/// </summary>
		protected MessageLcd4(): base()
		{}
		
		/// <summary>
		/// Creates a new MessageLcd4 for incoming PDUs.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to decode.</param>
		protected MessageLcd4(byte[] incomingBytes): base(incomingBytes)
		{}
		
		#endregion constructors
	}
}
