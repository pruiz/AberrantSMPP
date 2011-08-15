/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 * Copyright (C) 2010, 2011 Pablo Ruiz García <pruiz@crt0.net>
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
using System.Text;

namespace AberrantSMPP.Utility
{
	/// <summary>
	/// Catchall utility class for dealing with strings in SMPP packets.
	/// </summary>
	public class SmppStringUtil
	{
		/// <summary>
		/// Do not instantiate.
		/// </summary>
		private SmppStringUtil()
		{
		}
		
		/// <summary>
		/// Gets the next string from the given byte array.  Note: this
		/// method also trims off the string from the byte array, so that it can be easily 
		/// used in subsequent operations.  You have to know the string length; this will not 
		/// look for a null character(as it is meant to be used with non-null terminated 
		/// strings).
		/// </summary>
		/// <param name="remainder">The byte array to pull the string from.
		/// This should not contain extraneous data.</param>
		/// <param name="startIndex">The index to start getting the string from.
		/// Usually zero.</param>
		/// <param name="endIndex">The index to end at.  The character at
		/// this index will not be included.</param>
		/// <returns>The string retrieved.</returns>
		public static string GetStringFromBody(ref byte[] remainder, int startIndex, int endIndex)
		{
			string result;
			
			//make sure we aren't trying to read non-command
			//data
			if(endIndex <= remainder.Length)
			{
				result = System.Text.Encoding.ASCII.GetString(remainder, startIndex, endIndex - startIndex);
			}
			else
			{
				result = string.Empty;
			}
			
			//now trim down the remainder-no null character
			long length = remainder.Length - endIndex;
			if(length >= 0)
			{
				byte[] newRemainder = new byte[length];
				Array.Copy(remainder, endIndex, newRemainder, 0, length);
				remainder = newRemainder;
				newRemainder = null;
			}
			
			return result;
		}
		
		/// <summary>
		/// Gets the next null-terminated string from the given byte array.  Note: this
		/// method also trims off the string(including the null character)from the
		/// byte array, so that it can be easily used in subsequent operations.
		/// This is equivalent to GetCStringFromBody(ref remainder, 0).
		/// </summary>
		/// <param name="remainder">The byte array to
		/// pull the string from.  This should not
		/// contain extraneous data.</param>
		/// <returns>The string retrieved.</returns>
		public static string GetCStringFromBody(ref byte[] remainder)
		{
			return GetCStringFromBody(ref remainder, 0);
		}
		
		/// <summary>
		/// Gets the next null-terminated string from the given byte array.  Note: this
		/// method also trims off the string(including the null character)from the
		/// byte array, so that it can be easily used in subsequent operations.
		/// </summary>
		/// <param name="remainder">The byte array to
		/// pull the string from.  This should not
		/// contain extraneous data.</param>
		/// <param name="startIndex">The index to start getting the string from.
		/// Usually zero.</param>
		/// <returns>The string retrieved.</returns>
		public static string GetCStringFromBody(ref byte[] remainder, int startIndex)
		{
			int i;
			
			//find where the null character(end of string)is
			//this stops ON the null character
			for(i = startIndex; i < remainder.Length && remainder[i] != 0x00; i++)
			{
				;
			}
			
			return GetCStringFromBody(ref remainder, startIndex, i);
		}
		
		/// <summary>
		/// Gets the next null-terminated string from the given byte array.  Note: this
		/// method also trims off the string from the byte array, so that it can be easily used 
		/// in subsequent operations.
		/// </summary>
		/// <param name="remainder">The byte array to pull the string from.
		/// This should not contain extraneous data.</param>
		/// <param name="startIndex">The index to start getting the string from.
		/// Usually zero.</param>
		/// <param name="endIndex">The index to end at.  The character at
		/// this index will not be included.</param>
		/// <returns>The string retrieved.</returns>
		public static string GetCStringFromBody(ref byte[] remainder, int startIndex, int endIndex)
		{
			string result;
			
			//make sure we aren't trying to read non-command
			//data
			if(endIndex <= remainder.Length)
			{
				result = Encoding.ASCII.GetString(remainder, startIndex, endIndex - startIndex);
			}
			else
			{
				result = string.Empty;
			}
			
			//now trim down the remainder-chop off the null character as well
			long length = remainder.Length - endIndex - 1;
			if(length >= 0)
			{
				byte[] newRemainder = new byte[length];
				Array.Copy(remainder, endIndex + 1, newRemainder, 0, length);
				remainder = newRemainder;
				newRemainder = null;
			}
			
			return result;
		}
		
		/// <summary>
		/// Copies the data from the source array, appending a null character to the end.
		/// This really only has a use for creating null terminated arrays that represent
		/// strings.
		/// </summary>
		/// <param name="source">The source array to copy from.</param>
		/// <returns></returns>
		public static byte[] ArrayCopyWithNull(byte[] source)
		{
			byte[] temp = new byte[source.Length + 1];
			for(int i = 0; i < source.Length; i++)
			{
				temp[i] = source[i];
			}
			//append the null
			temp[temp.Length - 1] =(byte)0x00;
			
			return temp;
		}
	}
}
