using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace AberrantSMPP.Utility
{
    // XXX: NetFx does not yet support Interlocked.Increment for unsigned types. (pruiz)
    public static class InterlockedEx
    {
        /// <summary>
        /// unsigned equivalent of <see cref="Interlocked.Increment(ref Int32)"/>
        /// </summary>
        public static uint Increment(ref uint location)
        {
            var result = Interlocked.Increment(ref Unsafe.As<uint, int>(ref location));
            return unchecked((uint)result); //Unsafe.As<int, uint>(ref incrementedSigned);
        }
        
        /// <summary>
        /// unsigned equivalent of <see cref="Interlocked.Exchange(ref Int32, Int32)"/>
        /// </summary>
        public static uint Exchange(ref uint location, uint value)
        {
            var result = Interlocked.Exchange(ref Unsafe.As<uint, int>(ref location), unchecked((int)value));
            return unchecked((uint)result); //Unsafe.As<int, uint>(ref incrementedSigned);
        }
    }
}