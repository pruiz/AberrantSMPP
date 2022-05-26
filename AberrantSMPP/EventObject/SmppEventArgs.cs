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
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace AberrantSMPP.EventObjects 
{
	/// <summary>
	/// Base class to provide some functionality for all events.
	/// </summary>
	public abstract class SmppEventArgs : EventArgs
	{
		/// <summary>
		/// Sets up the SmppEventArgs.
		/// </summary>
		public SmppEventArgs()
		{
		}
	}
	
	/// <summary>
	/// Base class to provide some functionality for error events.
	/// </summary>
	public class SmppExceptionEventArgs : EventArgs
	{
		/// <summary>
		/// Allows access to the underlying Pdu.
		/// </summary>
		public Pdu Packet { get; }
		
		/// <summary>
		/// The thrown exception.
		/// </summary>
		public Exception ThrownException { get; }

		/// <summary>
		/// Sets up the SmppErrorEventArgs.
		/// </summary>
		public SmppExceptionEventArgs(Exception ex)
		{
			ThrownException = ex;
		}
		
		public SmppExceptionEventArgs(Pdu packet, Exception ex)
		{
			Packet = packet;
			ThrownException = ex;
		}
	}
	
	/// <summary>
	/// Base class to provide some functionality for all Pdu-related events.
	/// </summary>
	public abstract class SmppPacketEventArgs : EventArgs
	{
		/// <summary>
		/// Allows access to the underlying Pdu.
		/// </summary>
		public Pdu Packet { get; }
		
		/// <summary>
		/// Sets up the SmppEventArgs.
		/// </summary>
		/// <param name="packet">The SMPP Pdu.</param>
		public SmppPacketEventArgs(Pdu packet)
		{
			Packet = packet;
		}
	}
	
	/// <summary>
	/// Base class to provide some functionality for all events related to SmppRequests.
	/// </summary>
	public abstract class SmppPacketRequestEventArgs<T> : SmppPacketEventArgs 
		where T : SmppRequest
	{
		/// <summary>
		/// Allows access to the underlying Pdu.
		/// </summary>
		public T Request { get; }

		/// <summary>
		/// Sets up the SmppEventArgs.
		/// </summary>
		/// <param name="request">The SmppRequest.</param>
		public SmppPacketRequestEventArgs(T request)
			: base(request)
		{
			Request = request;
		}
	}
	
	/// <summary>
	/// Base class to provide some functionality for all events related to SmppResponses.
	/// </summary>
	public abstract class SmppPacketResponseEventArgs<T> : SmppPacketEventArgs 
		where T : SmppResponse
	{
		/// <summary>
		/// Allows access to the underlying Pdu.
		/// </summary>
		public T Response { get; }

		/// <summary>
		/// Sets up the SmppEventArgs.
		/// </summary>
		/// <param name="response">The SmppResponse.</param>
		public SmppPacketResponseEventArgs(T response)
			: base(response)
		{
			Response = response;
		}
	}
}
