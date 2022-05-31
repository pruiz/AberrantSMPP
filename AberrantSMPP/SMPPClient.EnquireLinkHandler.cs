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
            private readonly TimeSpan _interval;
            private bool _bound = false;
            public EnquireLinkHandler(TimeSpan interval)
                : base(false, interval, TimeSpan.Zero, interval)
            {
                _interval = interval;
            }

            protected override void ChannelIdle(IChannelHandlerContext context, IdleStateEvent stateEvent)
            {
                Guard.Argument(stateEvent).NotNull();

                if (_interval <= TimeSpan.Zero)
                    return;

                if (!_bound)
                    return;
                
                ReferenceCountUtil.SafeRelease(stateEvent);
                context.WriteAndFlushAsync(new SmppEnquireLink());
            }

            public override void UserEventTriggered(IChannelHandlerContext context, object evt)
            {
                if (evt is StateChangedEvent @event)
                {
                    _bound = @event.NewState == States.Bound;
                }
            }
        }
    }
}