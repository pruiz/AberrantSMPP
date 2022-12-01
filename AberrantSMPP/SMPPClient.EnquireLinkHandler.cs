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
            public EnquireLinkHandler(SMPPClient client)
                : base(false, client.EnquireLinkInterval, TimeSpan.Zero, TimeSpan.Zero)
            {
                _client = client;
            }

            protected override void ChannelIdle(IChannelHandlerContext context, IdleStateEvent stateEvent)
            {
                Guard.Argument(stateEvent).NotNull();

                _Log.DebugFormat("ChannelIdle(context:{0}, stateEvent:{1}, client.EnquiereLinkInterval:{2}, client.State:{3}",
                    context.Name, stateEvent.State, _client.EnquireLinkInterval, _client.State);

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
