using System;
using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace AberrantSMPP.Packet
{
    public class PduCodec : MessageToMessageCodec<IByteBuffer, Pdu>
    {
        public override bool IsSharable => true;

        protected override void Encode(IChannelHandlerContext ctx, Pdu msg, List<object> output)
        {
            if (msg is null)
                return;

            var bytes = msg.GetEncodedPdu(); //< OPTIMIZE: Pass buffer to write to..
            var buf = ctx.Allocator.Buffer(bytes.Length);
            buf.WriteBytes(bytes);
            output.Add(buf);
        }

        protected override void Decode(IChannelHandlerContext ctx, IByteBuffer msg, List<object> output)
        {
            var bytes = new byte[msg.ReadableBytes]; 
            msg.GetBytes(msg.ReaderIndex, bytes);
            output.Add(Pdu.Parse(bytes));
        }
    }
}