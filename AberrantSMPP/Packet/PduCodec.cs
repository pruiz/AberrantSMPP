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