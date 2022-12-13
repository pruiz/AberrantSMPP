﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Handlers.Tls
{
    using DotNetty.Buffers;

    using System;
    using System.IO;

    partial class MonoTlsHandler
    {
        abstract class MediationStreamBase : Stream
        {
            protected readonly MonoTlsHandler owner;

            public MediationStreamBase(MonoTlsHandler owner)
            {
                this.owner = owner;
            }
            
            public static MediationStreamBase Create(MonoTlsHandler owner)
            {
#if NET5_0_OR_GREATER
                return new MonoTlsHandler.MediationStreamNet(owner);
#else
                return new MediationStream(owner);
#endif
            }

            public abstract int TotalReadableBytes { get; }

            public abstract bool SourceIsReadable { get; }
            public abstract int SourceReadableBytes { get; }

            public abstract void SetSource(byte[] source, int offset, IByteBufferAllocator alloc);
            public abstract void ExpandSource(int count);
            public abstract void ResetSource(IByteBufferAllocator alloc);

            #region plumbing

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length
            {
                get { throw new NotSupportedException(); }
            }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            #endregion
        }
    }
}