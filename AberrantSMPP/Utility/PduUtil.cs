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
		
		private const int SUBADDRESS_MIN = 2;
		private const int SUBADDRESS_MAX = 23;
		
		#endregion constants

		/// <summary>
		/// Do not instantiate
		/// </summary>
		private PduUtil()
		{
		}

		public static byte[] GetEncodedText(DataCoding coding, string text)
		{
			switch (coding)
			{
				case DataCoding.SMSCDefault:
					//return GSM7BitEncoding.GetBytes(text);
				case DataCoding.OctetUnspecifiedA:
				case DataCoding.OctetUnspecifiedB:
					return new GSMEncoding().GetBytes(text);
				case DataCoding.IA5_ASCII:
					return Encoding.ASCII.GetBytes(text);
				case DataCoding.Latin1:
					return Encoding.GetEncoding("iso-8859-1").GetBytes(text);
				case DataCoding.JIS:
				case DataCoding.ExtendedKanjiJIS:
					return Encoding.GetEncoding("EUC-JP").GetBytes(text);
				case DataCoding.Cyrillic:
					return Encoding.GetEncoding("iso-8859-5").GetBytes(text);
				case DataCoding.Latin_Hebrew:
					return Encoding.GetEncoding("iso-8859-8").GetBytes(text);
				case DataCoding.UCS2:
					// 1201 == Unicode Big Endian (FFFE)!
					return Encoding.GetEncoding(1201).GetBytes(text);
				case DataCoding.MusicCodes:
					return Encoding.GetEncoding("iso-2022-jp").GetBytes(text);
				case DataCoding.KS_C:
					return Encoding.GetEncoding("ks_c_5601-1987").GetBytes(text);
				default:
					throw new ArgumentException("Invalid (or unsupported) DataCoding value.");
			}
		}
		
		/// <summary>
		/// Inserts the short message into the PDU ArrayList.
		/// </summary>
		/// <param name="pdu">The PDU to put the short message into.</param>
		/// <param name="ShortMessage">The short message to insert.</param>
		/// <returns>The length of the short message.</returns>
		public static byte InsertShortMessage(ArrayList pdu, DataCoding coding, object ShortMessage)
		{
			byte[] msg;
			
			if(ShortMessage == null)
			{
				msg = new Byte[]{0x00};
			}
			else if(ShortMessage is byte[])
			{
				msg =(byte[])ShortMessage;
			}
			else if (ShortMessage is string)
			{
				msg = GetEncodedText(coding, ShortMessage as string);
			}
			else
			{
				throw new ArgumentException("Short Message must be a string or byte array.");
			}
			//			if(msg.Length >= MessageLcd2.SHORT_MESSAGE_LIMIT)
			//				throw new ArgumentException(
			//					"Short message cannot be longer than " +
			//					MessageLcd2.SHORT_MESSAGE_LIMIT + " octets.");
			
			byte SmLength = (byte)msg.Length;
			pdu.Add(SmLength);
			pdu.AddRange(msg);
			
			return SmLength;
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a receipted message ID into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetReceiptedMessageId(Pdu pdu, string val)
		{
			const int MAX_RECEIPTED_ID_LEN = 65;
			if(val == null)
			{
				pdu.SetOptionalParamString(
					(ushort)Pdu.OptionalParamCodes.receipted_message_id, string.Empty);
			}
			else if(val.Length <= MAX_RECEIPTED_ID_LEN)
			{
				pdu.SetOptionalParamString(
					(ushort)Pdu.OptionalParamCodes.receipted_message_id, val);
			}
			else
			{
				throw new ArgumentException(
					"receipted_message_id must have length 1-" + MAX_RECEIPTED_ID_LEN);
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a network error code into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetNetworkErrorCode(Pdu pdu, string val)
		{
			const int ERR_CODE_LEN = 3;
			if(val == null || val.Length != ERR_CODE_LEN)
			{
				throw new ArgumentException("network_error_code must have length " + ERR_CODE_LEN);
			}
			else
			{
				pdu.SetOptionalParamString(
					(ushort)Pdu.OptionalParamCodes.network_error_code,val);
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts ITS session info into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetItsSessionInfo(Pdu pdu, string val)
		{
			const int MAX_ITS = 16;
			
			if(val == null)
			{
				pdu.SetOptionalParamString(
					(ushort)Pdu.OptionalParamCodes.its_session_info, string.Empty);
			}
			else if(val.Length == MAX_ITS)
			{
				pdu.SetOptionalParamString(
					(ushort)Pdu.OptionalParamCodes.its_session_info, val);
			}
			else
			{
				throw new ArgumentException("its_session_info must have length " + MAX_ITS);
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a destination subaddress into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetDestSubaddress(Pdu pdu, string val)
		{
			if(val.Length >= SUBADDRESS_MIN && val.Length <= SUBADDRESS_MAX)
			{
				pdu.SetOptionalParamString(
					(ushort)Pdu.OptionalParamCodes.dest_subaddress, val);
			}
			else
			{
				throw new ArgumentException(
					"Destination subaddress must be between " + SUBADDRESS_MIN + 
					" and " + SUBADDRESS_MAX + " characters.");
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a source subaddress into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetSourceSubaddress(Pdu pdu, string val)
		{
			if(val.Length >= SUBADDRESS_MIN && val.Length <= SUBADDRESS_MAX)
			{
				pdu.SetOptionalParamString(
					(ushort)Pdu.OptionalParamCodes.source_subaddress, val);
			}
			else
			{
				throw new ArgumentException(
					"Source subaddress must be between " + SUBADDRESS_MIN + 
					" and " + SUBADDRESS_MAX + " characters.");
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a callback number into the TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetCallbackNum(Pdu pdu, string val)
		{
			const int CALLBACK_NUM_MIN = 4;
			const int CALLBACK_NUM_MAX = 19;
			if(val.Length >= CALLBACK_NUM_MIN && val.Length <= CALLBACK_NUM_MAX)
			{
				pdu.SetOptionalParamString(
					(ushort)Pdu.OptionalParamCodes.callback_num, val);
			}
			else
			{
				throw new ArgumentException(
					"callback_num size must be between " + CALLBACK_NUM_MIN + 
					" and " + CALLBACK_NUM_MAX + " characters.");
			}
		}
		
		/// <summary>
		/// Takes the given PDU and inserts a message payload into its TLV table.
		/// </summary>
		/// <param name="pdu">The PDU to operate on.</param>
		/// <param name="val">The value to insert.</param>
		public static void SetMessagePayload(Pdu pdu, object val)
		{
			if(val != null)
				{
					byte[] encodedValue;
					if(val is string)
					{
						encodedValue = Encoding.ASCII.GetBytes((string)val);
					}
					else if(val is byte[])
					{
						encodedValue =(byte[])val;
					}
					else
					{
						throw new ArgumentException("Message Payload must be a string or byte array.");
					}
					
					const int MAX_PAYLOAD_LENGTH = 64000;
					if(encodedValue.Length < MAX_PAYLOAD_LENGTH)
					{
						pdu.SetOptionalParamBytes(
							(ushort)Pdu.OptionalParamCodes.message_payload, encodedValue);
					}
					else
					{
						throw new ArgumentException(
							"Message Payload must be " + MAX_PAYLOAD_LENGTH + " characters or less in size.");
					}
				}
				else
				{
					pdu.SetOptionalParamBytes(
						(ushort)Pdu.OptionalParamCodes.message_payload, new byte[]{0});
				}
		}
	}
}
