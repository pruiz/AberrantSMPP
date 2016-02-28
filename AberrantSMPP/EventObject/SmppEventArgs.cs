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


using AberrantSMPP.Packet;

namespace AberrantSMPP.EventObjects 
{
	/// <summary>
	/// Base class to provide some functionality for all events.
	/// </summary>
	public abstract class SmppEventArgs<T> : System.EventArgs 
		where T : Pdu
	{
		private T _response;

		/// <summary>
		/// Allows access to the underlying Pdu.
		/// </summary>
		public T ResponsePdu
		{
			get
			{
				return _response;
			}
		}

		/// <summary>
		/// Sets up the SmppEventArgs.
		/// </summary>
		/// <param name="response">The SMPPResponse.</param>
		public SmppEventArgs(T response)
		{
			_response = response;
		}
	}

	/// <summary>
	/// Base class to provide some functionality for all events.
	/// </summary>
	public abstract class SmppEventArgs : SmppEventArgs<Pdu>
	{
		public SmppEventArgs(Pdu response)
			: base(response)
		{
		}
	}
}
