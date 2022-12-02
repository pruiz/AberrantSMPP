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

                if (_client.State != States.Bound)
                    return;

                ReferenceCountUtil.SafeRelease(stateEvent);
                context.WriteAndFlushAsync(new SmppEnquireLink());
            }
        }
    }
}
