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
using AberrantSMPP.Packet.Request;


namespace AberrantSMPP.EventObjects 
{
	/// <summary>
	/// Class that defines a data_sm event.  
	/// </summary>
	public class DataSmEventArgs : SmppEventArgs 
	{
		private SmppDataSm _response;

		/// <summary>
		/// Allows access to the underlying Pdu.
		/// </summary>
		public SmppDataSm DataSmPdu
		{
			get
			{
				return _response;
			}
		}
		
		/// <summary>
		/// Creates a DataSmEventArgs.
		/// </summary>
		/// <param name="packet">The PDU that was received.</param>
		internal DataSmEventArgs(SmppDataSm packet): base(packet)
		{
			_response = packet;
		}
	}
}
