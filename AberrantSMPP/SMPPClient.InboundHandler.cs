using System;
using System.Buffers;
using System.Threading;

using DotNetty.Transport.Channels;

using Dawn;

using AberrantSMPP.EventObjects;
using AberrantSMPP.Packet;

namespace AberrantSMPP
{
    public partial class SMPPClient
    {
        private class InboundHandler : SimpleChannelInboundHandler<Pdu>
        {
            private SMPPClient _client;

            public InboundHandler(SMPPClient owner)
            {
                _client = Guard.Argument(owner, nameof(owner)).NotNull();
            }

            protected override void ChannelRead0(IChannelHandlerContext ctx, Pdu msg)
            {
                try
                {
                    _client.ProcessPdu(msg);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("RECEIVE ERROR: " + exception.ToString());
                    _client.DispatchOnError(new CommonErrorEventArgs(exception));
                }
            }

            public override void ChannelInactive(IChannelHandlerContext context)
            {
                base.ChannelInactive(context);

                lock (_client._bindingLock)
                {
                    _Log.Warn("Socket closed, scheduling a rebind operation.");
                    _client._ReBindRequired = true;
                }

                _client.DispatchOnClose(new EventArgs());
            }

            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
            {
                _client.DispatchOnError(new CommonErrorEventArgs(exception));
            }
        }
    }
}