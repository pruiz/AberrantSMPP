using System;
using System.Runtime.InteropServices;

using DotNetty.Transport.Channels;

namespace AberrantSMPP
{
    public partial class SMPPClient
    {
        /// <summary>
        /// Handler intended for automatically (re-)connecting / (re-)binding sessions. 
        /// </summary>
        public class ResilientHandler : ChannelHandlerAdapter
        {
            private readonly SMPPClient _client;

            public ResilientHandler(SMPPClient client)
            {
                _client = client;
            }

            /*public override void ChannelRegistered(IChannelHandlerContext context)
            {
                base.ChannelRegistered(context);

                if (_client.RestablishInterval.Ticks > 0)
                {
                    _Log.InfoFormat("Trying to connect (resilient) session..");
                    _client.Connect(); //< FIXME: Reschedule if failed..
                }
            }*/

            public override void ChannelActive(IChannelHandlerContext context)
            {
                base.ChannelActive(context);
                
                if (_client.RestablishInterval.Ticks > 0)
                {
                    _Log.InfoFormat("Trying to bind (resilient) session..");
                    _client.Bind(); //< FIXME: Reschedule if failed..
                }
            }

            public override void ChannelInactive(IChannelHandlerContext context)
            {
                base.ChannelInactive(context);

                if (_client.RestablishInterval.Ticks > 0)
                {
                    _Log.InfoFormat("Scheduling restablishment of session after {0}..", _client.RestablishInterval);
                    context.Channel.EventLoop.Parent.Schedule(x => (x as SMPPClient).Start(_client.RestablishInterval), _client, _client.RestablishInterval);
                }
            }
        }
    }
}