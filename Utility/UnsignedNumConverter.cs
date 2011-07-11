/* RoaminSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 *
 * This file is part of RoaminSMPP.
 *
 * RoaminSMPP is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, version 3 of the License.
 *
 * RoaminSMPP is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lessert General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;

namespace RoaminSMPP.Utility
{
	/// <summary>
	/// Utility class to do big-endian to little endian conversions and the like.
	/// This is for unsigned numbers only; use the IPAddress class for signed values.
	/// </summary>
	public sealed class UnsignedNumConverter
	{
		/// <summary>
		/// Do not instantiate.
		/// </summary>
		private UnsignedNumConverter()
		{
		}
		
		/// <summary>
		/// Converts from big-endian to little endian and vice versa.
		/// </summary>
		/// <param name="val">The value to swap.</param>
		/// <returns>The byte-swapped value, 0 if val &lt; 0</returns>
		public static UInt32 SwapByteOrdering(UInt32 val)
		{
			if(val < 0)
				return 0;
			return((val << 24)& 0xFF000000)+((val << 8)& 0x00FF0000)+
					((val >> 8)& 0x0000FF00)+((val >> 24)& 0x000000FF);
		}
		
		/// <summary>
		/// Converts from big-endian to little endian and vice versa.  Don't use
		/// for negative integers; it has not been tested for that.
		/// </summary>
		/// <param name="val">The value to swap.</param>
		/// <returns>The byte-swapped value, 0 if val &lt; 0</returns>
		public static UInt16 SwapByteOrdering(UInt16 val)
		{
			if(val < 0)
			{
				return 0;
			}
			
			UInt16 newVal = (UInt16)(((val << 8)& 0xFF00)+((val >> 8)& 0x00FF));
			
			return newVal;
		}
	}
}
