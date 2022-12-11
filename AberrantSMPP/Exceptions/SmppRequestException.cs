/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2010-2022 Pablo Ruiz García <pruiz@netway.org>
 *
 * This file is part of AberrantSMPP.
 *
 * AberrantSMPP is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, version 3 of the License.
 *
 * AberrantSMPP is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with AberrantSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace AberrantSMPP.Exceptions
{
	/// <summary>
	/// Remote party reported an error to our request.
	/// </summary>
	[Serializable]
	public class SmppRequestException : Exception
	{
		public SmppRequest Request { get; set; }
		public SmppResponse Response { get; set; }
		public CommandStatus CommandStatus { get; set; }

		protected SmppRequestException() { }
		protected SmppRequestException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }

		public SmppRequestException(string message, CommandStatus status) 
			: base(message) 
		{
			CommandStatus = status;
		}

		public SmppRequestException(string message, SmppRequest request, SmppResponse response)
			: base(message)
		{
			if (request == null) throw new ArgumentNullException("request");
			if (response == null) throw new ArgumentNullException("response");

			Request = request;
			Response = response;
			CommandStatus = response.CommandStatus;
		}
	}
}
