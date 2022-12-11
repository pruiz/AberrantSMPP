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

using AberrantSMPP.Packet.Request;

using Dawn;

using DotNetty.Common.Utilities;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;

namespace AberrantSMPP
{
	public partial class SMPPClient
	{
		private class EnquireLinkHandler : IdleStateHandler
		{
			private readonly SMPPClient _client;

			public EnquireLinkHandler(SMPPClient owner)
				: base(false, owner.EnquireLinkInterval, TimeSpan.Zero, TimeSpan.Zero)
			{
				base.EnsureNotSharable();

				_client = Guard.Argument(owner, nameof(owner)).NotNull();
			}

			protected override void ChannelIdle(IChannelHandlerContext context, IdleStateEvent stateEvent)
			{
				Guard.Argument(stateEvent).NotNull();

				if (_client.EnquireLinkInterval <= TimeSpan.Zero)
					return;

				// SMPPv5.0 -- 2.4. Operation Matrix
				// Once connected, enquire request can be sent on any state
				if (_client.State < States.Connected)
					return;

				ReferenceCountUtil.SafeRelease(stateEvent);
				context.WriteAndFlushAsync(new SmppEnquireLink());
			}
		}
	}
}
