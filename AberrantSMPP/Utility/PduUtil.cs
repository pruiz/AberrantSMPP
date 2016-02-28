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
using System.Text;

using AberrantSMPP.Packet;

namespace AberrantSMPP.Utility
{
	/// <summary>
	/// Holds common functionality for requests.
	/// </summary>
	public class PduUtil
	{			
		#region constants
		
		private const int SubaddressMin = 2;
		private const int SubaddressMax = 23;
		
		#endregion constants

		/// <summary>
		/// Do not instantiate
		/// </summary>
		private PduUtil()
		{
		}

		/// <summary>
		/// Gets the encoded representation of the specified text.
		/// </summary>
		/// <param name="coding">The coding.</param>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		public static byte[] GetEncodedText(DataCoding coding, string text)
		{
			switch (coding)
			{
				case DataCoding.SmscDefault:
					//return GSM7BitEncoding.GetBytes(text);
				case DataCoding.OctetUnspecifiedA:
				case DataCoding.OctetUnspecifiedB:
					return new GsmEncoding(true, false).GetBytes(text);
				case DataCoding.Ia5Ascii:
					return Encoding.ASCII.GetBytes(text);
				case DataCoding.Latin1:
					return Encoding.GetEncoding("iso-8859-1").GetBytes(text);
				case DataCoding.Jis:
				case DataCoding.ExtendedKanjiJis:
					return Encoding.GetEncoding("EUC-JP").GetBytes(text);
				case DataCoding.Cyrillic:
					return Encoding.GetEncoding("iso-8859-5").GetBytes(text);
				case DataCoding.LatinHebrew:
					return Encoding.GetEncoding("iso-8859-8").GetBytes(text);
				case DataCoding.Ucs2:
					// 1201 == Unicode Big Endian (FFFE)!
					return Encoding.GetEncoding(1201).GetBytes(text);
				case DataCoding.MusicCodes:
					return Encoding.GetEncoding("iso-2022-jp").GetBytes(text);
				case DataCoding.KsC:
					return Encoding.GetEncoding("ks_c_5601-1987").GetBytes(text);
				default:
					throw new ArgumentException("Invalid (or unsupported) DataCoding value.");
			}
		}

		/// <summary>
		/// Gets the decoded representation of the specified data.
		/// </summary>
		/// <param name="coding">The coding.</param>
		/// <param name="data">The data.</param>
		/// <returns></returns>
		public static string GetDecodedText(DataCoding coding, byte[] data)
		{
			switch (coding)
			{
				case DataCoding.SmscDefault:
				//return GSM7BitEncoding.GetBytes(text);
				case DataCoding.OctetUnspecifiedA:
				case DataCoding.OctetUnspecifiedB:
					return new GsmEncoding().GetString(data);
				case DataCoding.Ia5Ascii:
					return Encoding.ASCII.GetString(data);
				case DataCoding.Latin1:
					return Encoding.GetEncoding("iso-8859-1").GetString(data);
				case DataCoding.Jis:
				case DataCoding.ExtendedKanjiJis:
					return Encoding.GetEncoding("EUC-JP").GetString(data);
				case DataCoding.Cyrillic:
					return Encoding.GetEncoding("iso-8859-5").GetString(data);
				case DataCoding.LatinHebrew:
					return Encoding.GetEncoding("iso-8859-8").GetString(data);
				case DataCoding.Ucs2:
					// 1201 == Unicode Big Endian (FFFE)!
					return Encoding.GetEncoding(1201).GetString(data);
				case DataCoding.MusicCodes:
					return Encoding.GetEncoding("iso-2022-jp").GetString(data);
				case DataCoding.KsC:
					return Encoding.GetEncoding("ks_c_5601-1987").GetString(data);
				default:
					throw new ArgumentException("Invalid (or unsupported) DataCoding value.");
			}
		}

		/// <summary>
		/// Inserts the short message into the PDU ArrayList.
		/// </summary>
		/// <param name="pdu">The PDU to put the short message into.</param>
		/// <param name="shortMessage">The short message to insert.</param>
		/// <returns>The length of the short message.</returns>
		public static byte InsertShortMessage(ArrayList pdu, DataCoding coding, object shortMessage)
		{
			byte[] msg;
			
			if(shortMessage == null)
			{
				msg = null;
			}
			else if(shortMessage is byte[])
			{
				msg =(byte[])shortMessage;
			}
			else if (shortMessage is string)
			{
				msg = GetEncodedText(coding, shortMessage as string);
			}
			else
			{
				throw new ArgumentException("Short Message must be a string or byte array.");
			}
			//			if(msg.Length >= MessageLcd2.SHORT_MESSAGE_LIMIT)
			//				throw new ArgumentException(
			//					"Short message cannot be longer than " +
			//					MessageLcd2.SHORT_MESSAGE_LIMIT + " octets.");
			
			byte smLength = msg == null ? (byte)0 : (byte)msg.Length;
			pdu.Add(smLength);
			if (msg != null) pdu.AddRange(msg);
			
			return smLength;
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a receipted message ID into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetReceiptedMessageId(Pdu pdu, string val)
		{
			const int maxReceiptedIdLen = 65;

			if (val == null || val.Length <= maxReceiptedIdLen)
			{
				pdu.SetOptionalParamString(OptionalParamCodes.ReceiptedMessageId, val, true);
			}
			else
			{
				throw new ArgumentException(
					"receipted_message_id must have length 1-" + maxReceiptedIdLen);
			}
		}

		/// <summary>
		/// Takes the given PDU and inserts a network error code into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="data">The binary encoded error (see spec).</param>
		public static void SetNetworkErrorCode(Pdu pdu, byte[] data)
		{
			const int errCodeLen = 3;
			if (data == null)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.NetworkErrorCode, null);
			}
			else if(data.Length != errCodeLen)
			{
				throw new ArgumentException("network_error_code must have length " + errCodeLen);
			}
			else
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.NetworkErrorCode, data);
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts ITS session info into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetItsSessionInfo(Pdu pdu, byte[] val)
		{
			const int maxIts = 16;
			
			if(val == null)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.ItsSessionInfo, null);
			}
			else if(val.Length == maxIts)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.ItsSessionInfo, val);
			}
			else
			{
				throw new ArgumentException("its_session_info must have length " + maxIts);
			}
		}

		/// <summary>
		/// Takes the given PDU and inserts a destination subaddress into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="data">The data (see spec.)</param>
		public static void SetDestSubaddress(Pdu pdu, byte[] data)
		{
			if (data == null)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.DestSubaddress, null);
			}
			else if(data.Length >= SubaddressMin && data.Length <= SubaddressMax)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.DestSubaddress, data);
			}
			else
			{
				throw new ArgumentException(
					"Destination subaddress must be between " + SubaddressMin + 
					" and " + SubaddressMax + " bytes.");
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a source subaddress into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetSourceSubaddress(Pdu pdu, byte[] data)
		{
			if (data == null)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.SourceSubaddress, null);
			}
			else if (data.Length >= SubaddressMin && data.Length <= SubaddressMax)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.SourceSubaddress, data);
			}
			else
			{
				throw new ArgumentException(
					"Source subaddress must be between " + SubaddressMin + 
					" and " + SubaddressMax + " bytes.");
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a callback number into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetCallbackNum(Pdu pdu, byte[] val)
		{
			const int callbackNumMin = 4;
			const int callbackNumMax = 19;

			if (val == null)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.CallbackNum, null);
			}
			else if(val.Length >= callbackNumMin && val.Length <= callbackNumMax)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.CallbackNum, val);
			}
			else
			{
				throw new ArgumentException(
					"callback_num size must be between " + callbackNumMin + 
					" and " + callbackNumMax + " characters.");
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a message payload into its TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetMessagePayload(Pdu pdu, DataCoding coding, object val)
		{
			byte[] encodedValue = null;

			if (val == null)
			{
				pdu.SetOptionalParamBytes(OptionalParamCodes.MessagePayload, null);
			}
			else if(val is string)
			{
				encodedValue = GetEncodedText(coding, val as string);
			}
			else if(val is byte[])
			{
				encodedValue =(byte[])val;
			}
			else
			{
				throw new ArgumentException("Message Payload must be a string or byte array.");
			}
			
			if (encodedValue != null) 
			{
				const int maxPayloadLength = 64000;
				if(encodedValue.Length < maxPayloadLength)
				{
					pdu.SetOptionalParamBytes(
						OptionalParamCodes.MessagePayload, encodedValue);
				}
				else
				{
					throw new ArgumentException(
						"Message Payload must be " + maxPayloadLength + " characters or less in size.");
				}
			}
		}
	}
}
