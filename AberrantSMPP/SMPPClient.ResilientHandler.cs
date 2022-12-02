using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dawn;

using DotNetty.Transport.Channels;

namespace AberrantSMPP
{
    public partial class SMPPClient
    {
        /// <summary>
        /// Handler intended for automatically (re-)connecting / (re-)binding sessions. 
        /// </summary>
        /// See:
        /// - https://github.com/tjakopan/DotNettyExamples/blob/master/Uptime.Client/UptimeClientHandler.cs
        /// - https://github.com/tjakopan/DotNettyExamples/blob/master/Uptime.Client/Program.cs
        /// - https://github.com/matrix-xmpp/matrix-vnext/blob/9d7407d41d0344495e6e84919fde2a2fdf971225/src/Matrix/Network/Handlers/ReconnectHandler.cs
        /// - https://github.com/hanxinimm/DotNetty.Nats/blob/9b7441554bc65ae9e50781fae6d69a6168162f55/Hunter.STAN.Client/Handlers/ReconnectChannelHandler.cs
        /// - https://github.com/hanxinimm/DotNetty.Nats/blob/433992c45dee6a9f2814b34c385c274bfe5cc275/Hunter.NATS.Client/Handlers/ReconnectChannelHandler.cs
        /// - https://github.com/soilmeal/soil-csharp/blob/09cf19c8cefd33dc3eb7a4d2496a49f9f8dd1fb4/src/Soil.Net/Channel/ReconnectableTcpSocketChannel.cs
        /// - https://github.com/zhige777/Test/blob/1024093f81f4945834c10b8ea80828aec473dac5/NettyTest/NettyTestClient/Program.cs
        /// - https://github.com/cocosip/FluentSocket/blob/6d331bc60f90565d945500931f09aa49612c92f3/framework/src/FluentSocket.DotNetty/DotNetty/DotNettySocketClient.cs
        /// - https://github.com/Azure/DotNetty/issues/245
        ///     - Note we could use CloseCompetion..
        public class ResilientHandler : ChannelHandlerAdapter
        {
            private readonly SMPPClient _client;

            public ResilientHandler(SMPPClient owner)
            {
                _client = Guard.Argument(owner, nameof(owner)).NotNull();
            }

            //public override void ChannelActive(IChannelHandlerContext context)
            //{
            //    base.ChannelActive(context);
            //}

            public override void ChannelInactive(IChannelHandlerContext context)
            {
                base.ChannelInactive(context);
                _client.ScheduleReconnect();
            }
        }
    }
}
