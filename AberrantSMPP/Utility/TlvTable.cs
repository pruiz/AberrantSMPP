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
using System.Collections;
using System.Net;
using System.Text;
using System.Diagnostics;

namespace AberrantSMPP.Utility
{
	/// <summary>
	/// Tag, length, value table for SMPP PDUs.  The methods in this class assume 
	/// that the tag passed in is already in network byte order.
	/// </summary>
	public class TlvTable
	{
		private Hashtable tlvTable;
		
		/// <summary>
		/// Creates a TLV table.
		/// </summary>
		public TlvTable()
		{
			tlvTable = new Hashtable();
			tlvTable = Hashtable.Synchronized(tlvTable);
		}
		
		/// <summary>
		/// Converts the TLV byte array data into the Hashtable.  This will check if
		/// the passed in data is null or is an empty array so you don't need to
		/// check it before calling this method.  This is equivalent to
		/// TranslateTlvDataIntoTable(tlvData, 0).
		/// </summary>
		/// <param name="tlvData">The bytes of TLVs.</param>
		public void TranslateTlvDataIntoTable(byte[] tlvData)
		{
			TranslateTlvDataIntoTable(tlvData, 0);
		}
		
		/// <summary>
		/// Converts the TLV byte array data into the Hashtable.  This will check if
		/// the passed in data is null or is an empty array so you don't need to check
		/// it before calling this method.
		/// </summary>
		/// <param name="tlvData">The bytes of TLVs.</param>
		/// <param name="index">The index of the byte array to start at.  This is here
		/// because in some instances you may not want to start at the
		/// beginning.</param>
		public void TranslateTlvDataIntoTable(byte[] tlvData, Int32 index)
		{
			if(tlvData == null || tlvData.Length <= 0)
			{
				return;
			}
			
			//go through and decode the TLVs
			while(index < tlvData.Length)
			{
				InjectTlv(tlvData, ref index);
			}
		}
		
		/// <summary>
		/// Using the given tlvData byte array and the given starting index, inserts
		/// the tag and value(as a byte array)into the hashtable.  Note that the
		/// tlvData needs to be in the SMPP v3.4 format(tag, length, value).  This 
		/// assumes that the tag and length (from the tlvData array) are in network byte order.
		///
		/// Note also that this will advance the index by the TLV data length so that
		/// it may be used for consecutive reads from the same array.
		/// </summary>
		/// <param name="tlvData">The TLV data as a byte array.</param>
		/// <param name="index">The index of the array to start reading from.</param>
		private void InjectTlv(byte[] tlvData, ref Int32 index)
		{
			byte[] temp = new byte[2];
			temp[0] = tlvData[index];
			temp[1] = tlvData[index + 1];
			
			#if DEBUG
			Console.WriteLine("tag bytes " + temp[0].ToString("X").PadLeft(2, '0') + 
			                  temp[1].ToString("X").PadLeft(2, '0'));
			#endif
			
			UInt16 tag = BitConverter.ToUInt16(temp, 0);
			index += 2;
			temp[0] = tlvData[index];
			temp[1] = tlvData[index + 1];
			UInt16 length = UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt16(temp, 0));
			index += 2;
			//decode the value
			
			#if DEBUG
			Console.WriteLine("TLV Length " + length);
			#endif
			
			ArrayList data = new ArrayList(length);
			
			int total = index + length;
			for(int k = index;(k < index + length)&& k < tlvData.Length; k++)
			{
				data.Add(tlvData[k]);
			}
			
			data.TrimToSize();
			//add the values to the hashtable
			if (!tlvTable.ContainsKey(tag)) // Sometimes we receive mulitple tags.  http://smppapi.sourceforge.net/apidocs/ie/omk/smpp/message/tlv/TLVTable.html
			{
				tlvTable.Add(tag, data.ToArray(typeof(byte)));
			}

			//set it up for the next run
			index += length;
		}

#if false // Deprecated API
		/// <summary>
		/// Gets the optional parameter bytes associated with the given tag.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <returns>The optional parameter bytes, null if not found.</returns>
		/// <exception cref="ApplicationException">Thrown if the tag cannot be found.</exception>
		public byte[] GetOptionalParamBytes(UInt16 tag)
		{
			object val = tlvTable[tag];
			if(val == null)
			{
				throw new ApplicationException("TLV tag " + tag + " not found.");
			}
			else
			{
				byte[] bVal = (byte[])val;
#if DEBUG
				StringBuilder sb = new StringBuilder();
				sb.Append("Getting tag " + UnsignedNumConverter.SwapByteOrdering(tag));
				sb.Append("\nValue: ");
				
				for(int i = 0; i < bVal.Length; i++) 
				{
					sb.Append(bVal[i]);
					sb.Append(" ");
				}
				
				Console.WriteLine(sb);
				
#endif
				return bVal;
			}
		}
		
		/// <summary>
		/// Gets the optional parameter string associated with the given tag.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <returns>The optional parameter string, the empty string if not found.
		/// </returns>
		/// <exception cref="ApplicationException">Thrown if the tag cannot be found.</exception>
		public string GetOptionalParamString(UInt16 tag)
		{
			byte[] val = GetOptionalParamBytes(tag);
			
			return Encoding.ASCII.GetString(val);
		}
		
		/// <summary>
		/// Sets the given TLV(as a byte array)into the table.  This ignores null values.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void SetOptionalParamBytes(UInt16 tag, byte[] val)
		{
#if DEBUG
			StringBuilder sb = new StringBuilder();
			sb.Append("Setting tag " + UnsignedNumConverter.SwapByteOrdering(tag));
			sb.Append("\nValue: ");
			
			for(int i = 0; i < val.Length; i++) 
			{
				sb.Append(val[i]);
				sb.Append(" ");
			}
			
			Console.WriteLine(sb);
			
#endif
			if(val != null)
			{
				if(val.Length > UInt16.MaxValue)
				{
					throw new Exception("Optional parameter value for " + tag + " is too large.");
				}
				tlvTable.Add(tag, val);
			}
		}
		
		/// <summary>
		/// Sets the given TLV(as a string)into the table.  This ignores
		/// null values.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void SetOptionalParamString(UInt16 tag, string val)
		{
			if(val != null)
			{
				SetOptionalParamBytes(tag, Encoding.ASCII.GetBytes(val));
			}
		}
		
		/// <summary>
		/// Allows the updating of TLV values.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		/// <exception cref="ApplicationException">Thrown if the tag cannot be found.</exception>
		public void UpdateOptionalParamBytes(UInt16 tag, byte[] val)
		{
			object obj = tlvTable[tag];
			
			if(obj != null)
			{
				tlvTable[tag] = val;
			}
			else
			{
				throw new ApplicationException("TLV tag " + tag + " not found");
			}
		}
		
		/// <summary>
		/// Allows the updating of TLV values.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void UpdateOptionalParamString(UInt16 tag, string val)
		{
			UpdateOptionalParamBytes(tag, Encoding.ASCII.GetBytes(val));
		}
#endif

		#region NEW API
		/// <summary>
		/// Gets the bytes.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns></returns>
		public byte[] GetBytes(UInt16 tag)
		{
			if (!tlvTable.ContainsKey(tag) || tlvTable[tag] == null)
				throw new ApplicationException("TLV tag " + tag + " not found.");

			return (byte[])tlvTable[tag];
		}
		/// <summary>
		/// Gets the byte.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns></returns>
		public byte GetByte(UInt16 tag)
		{
			return GetBytes(tag)[0];
		}
		/// <summary>
		/// Sets the specified tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="value">The value.</param>
		public void Set(UInt16 tag, byte value)
		{
			Set(tag, new[] { value });
		}
		/// <summary>
		/// Sets the specified tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="value">The value.</param>
		public void Set(UInt16 tag, byte[] value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			if (value.Length > UInt16.MaxValue)
				throw new ArgumentException("Parameter value for tag '" + tag + "' is too large.");
			
			tlvTable[tag] = value;
		}
		/// <summary>
		/// Determines whether this TlvTable contains the specified key.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>
		/// 	<c>true</c> if contains the key; otherwise, <c>false</c>.
		/// </returns>
		public bool ContainsKey(UInt16 tag)
		{
			return tlvTable.ContainsKey(tag);
		}
		/// <summary>
		/// Removes the specified tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		public void Remove(UInt16 tag)
		{
			tlvTable.Remove(tag);
		}
		#endregion

		/// <summary>
		/// Iterates through the hashtable, gathering the tag, length, and
		/// value as it goes.  For each entry, it encodes the TLV into a byte
		/// array.  It is assumed that the tags and length are already in network 
		/// byte order.
		/// </summary>
		/// <returns>An ArrayList consisting of byte array entries, each of which
		/// is a TLV.  Returns an empty ArrayList if the TLV table is empty.</returns>
		public ArrayList GenerateByteEncodedTlv()
		{
			if(tlvTable == null || tlvTable.Count <= 0)
			{
				return new ArrayList(0);
			}
			
			ArrayList tlvs = new ArrayList();
			IDictionaryEnumerator iterator = tlvTable.GetEnumerator();
			ArrayList elem = new ArrayList();
			while(iterator.MoveNext())
			{
				elem.Clear();
				//tag-2 bytes
				elem.AddRange(BitConverter.GetBytes(((UInt16)iterator.Key)));
				//length-2 bytes
				byte[] nextVal = (byte[])iterator.Value;
				UInt16 tlvLength = UnsignedNumConverter.SwapByteOrdering(((UInt16)(nextVal).Length));
				elem.AddRange(BitConverter.GetBytes(tlvLength));
				//value
				elem.AddRange(nextVal);
				elem.TrimToSize();
				//copy it over to a byte array
				tlvs.Add(elem.ToArray(typeof(byte)));
			}
			tlvs.TrimToSize();
			
			return tlvs;
		}
	}
}
