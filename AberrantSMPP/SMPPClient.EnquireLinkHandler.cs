using System;
using Dawn;

using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using DotNetty.Common.Utilities;

using AberrantSMPP.Packet.Request;

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
