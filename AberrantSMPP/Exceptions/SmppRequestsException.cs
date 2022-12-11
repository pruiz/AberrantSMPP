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
using AberrantSMPP.Utility;
using Dawn;

namespace AberrantSMPP.Exceptions
{
    /// <summary>
    /// Remote party reported an error to our request.
    /// </summary>
    [Serializable]
    public class SmppRequestsException : AggregateException
    {
        public IEnumerable<SmppRequest> Requests { get; set; }
        public IEnumerable<SmppResponse> Responses { get; set; }

        protected SmppRequestsException() { }
        protected SmppRequestsException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }


        public SmppRequestsException(string message, IEnumerable<SmppRequestException> exceptions)
            : base(message, exceptions)
        {
            Guard.Argument(exceptions).NotNull().NotEmpty();

            Requests = exceptions.Select(x => x.Request).ToArray();
            Responses = exceptions.Select(x => x.Response).ToArray();
        }
        
        public SmppRequestsException(string message, IEnumerable<SmppRequest> requests, IEnumerable<SmppResponse> responses)
            : base(message)
        {
            Guard.Argument(requests).NotNull().NotEmpty();
            Guard.Argument(responses).NotNull().NotEmpty();

            Requests = requests.ToArray();
            Responses = responses.ToArray();
        }
    }
}