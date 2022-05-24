/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 * Copyright (C) 2010, 2011 Pablo Ruiz Garc?a <pruiz@crt0.net>
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
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Text;
using System.Collections;
using System.Diagnostics;

using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;
using AberrantSMPP.Utility;

using Dawn;
using DotNetty.Buffers;

namespace AberrantSMPP.Packet
{
	/// <summary>
	/// Represents a protocol data unit.  This class holds type enumerations and common 
	/// functionality for request and response Pdus.
	/// </summary>
	public abstract class Pdu : IDisposable
	{			
		#region constants
		
		/// <summary>
		/// Standard length of Pdu header.
		/// </summary>
		protected const int HEADER_LENGTH = 16;
		/// <summary>
		/// Delivery time length
		/// </summary>
		protected const int DATE_TIME_LENGTH = 16;
		
		#endregion constants
		
		#region private fields
		private CommandStatus _CommandStatus = 0;
		private CommandId _CommandID;
		private TlvTable _tlvTable = new TlvTable();
		private uint _SequenceNumber = 0;
		private uint _CommandLength;
		private byte[] _PacketBytes = null;
		#endregion private fields
		
		#region properties
		/// <summary>
		/// Gets the default command id to set when creating this kind of PDU.
		/// </summary>
		/// <value>The default command id.</value>
		protected abstract CommandId DefaultCommandId { get; }

		/// <summary>
		/// Defines the overall length of the Pdu in octets(i.e. bytes).
		/// </summary>
		public uint CommandLength
		{
			get
			{
				return _CommandLength;
			}
		}
		
		/// <summary>
		/// The sequence number of the message.  Only call this after you call 
		/// GetMSBHexEncoding; it will be incorrect otherwise.  If you are setting the 
		/// sequence number(such as for a deliver_sm_resp), set this before you call 
		/// GetMSBHexEncoding.  Note that setting the custom sequence number will 
		/// disable the automatic updating of the sequence number for this instance.  
		/// You can restore the automatic updating by setting this property to 0.
		/// </summary>
		public uint SequenceNumber
		{
			get
			{
				return _SequenceNumber;
			}
			set
			{
				_SequenceNumber = value;
			}
		}
		
		/// <summary>
		/// Indicates outcome of request.
		/// </summary>
		public CommandStatus CommandStatus
		{
			get
			{
				return _CommandStatus;
			}
			set
			{
				_CommandStatus = value;
			}
		}
		
		/// <summary>
		/// The command ID of this Pdu.
		/// </summary>
		protected CommandId CommandID
		{
			get
			{
				return _CommandID;
			}
			set
			{
				_CommandID = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the byte response from the SMSC.  This will return a clone of the
		/// byte array upon a "get" request.
		/// </summary>
		public byte[] PacketBytes
		{
			get
			{
				if (_PacketBytes == null) return null;

				return (byte[])_PacketBytes.Clone();
			}

			private set
			{
				_PacketBytes = value;
			}
		}

		#endregion properties
		
		#region constructors
		
		/// <summary>
		/// Constructor for sent Pdus.
		/// </summary>
		protected Pdu()
		{
			CommandStatus = 0;
			CommandID = DefaultCommandId;
		}
		
		/// <summary>
		/// Constructor for received Pdus.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to translate to a Pdu.</param>
		protected Pdu(byte[] incomingBytes)
		{
			_PacketBytes = Guard.Argument(incomingBytes, nameof(incomingBytes)).NotNull().MinCount(16);
			_CommandLength = DecodeCommandLength(_PacketBytes);
			_CommandID = DecodeCommandId(_PacketBytes);
			_CommandStatus = (CommandStatus)UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt32(_PacketBytes, 8));
			_SequenceNumber = UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt32(_PacketBytes, 12));

			Guard.Argument(incomingBytes).Require(x => x.Length == _CommandLength, x => "PDU Length mismatch?!");
			
			//set the other Pdu-specific fields
			DecodeSmscResponse();
		}
		
		/// <summary>
		/// Gets a single PDU based on the response bytes.
		/// </summary>
		/// <param name="response">The SMSC response.</param>
		/// <returns>The PDU corresponding to the bytes.</returns>
		public static Pdu Parse(byte[] response)
		{
			var commandID = DecodeCommandId(response);

			Pdu packet;
			switch(commandID)
			{
				case CommandId.alert_notification:
					packet = new SmppAlertNotification(response);
					break;
				case CommandId.bind_receiver_resp:
				case CommandId.bind_transceiver_resp:
				case CommandId.bind_transmitter_resp:
					packet = new SmppBindResp(response);
					break;
				case CommandId.cancel_sm_resp:
					packet = new SmppCancelSmResp(response);
					break;
				case CommandId.data_sm_resp:
					packet = new SmppDataSmResp(response);
					break;
				case CommandId.deliver_sm:
					packet = new SmppDeliverSm(response);
					break;
				case CommandId.enquire_link:
					packet = new SmppEnquireLink(response);
					break;
				case CommandId.enquire_link_resp:
					packet = new SmppEnquireLinkResp(response);
					break;
				case CommandId.outbind:
					packet = new SmppOutbind(response);
					break;
				case CommandId.query_sm_resp:
					packet = new SmppQuerySmResp(response);
					break;
				case CommandId.replace_sm_resp:
					packet = new SmppReplaceSmResp(response);
					break;
				case CommandId.submit_multi_resp:
					packet = new SmppSubmitMultiResp(response);
					break;
				case CommandId.submit_sm_resp:
					packet = new SmppSubmitSmResp(response);
					break;
				case CommandId.unbind_resp:
					packet = new SmppUnbindResp(response);
					break;
				case CommandId.generic_nack:
					packet = new SmppGenericNack(response);
					break;
				default:
					packet = null;
					break;
			}

			return packet;
		}

		#endregion constructors
		
		#region overridable methods

		///<summary>
		/// Gets the hex encoding(big-endian)of this Pdu.
		///</summary>
		///<return>The hex-encoded version of the Pdu</return>
		public byte[] GetEncodedPdu()
		{
			var pdu = GetPduHeader();
			AppendPduData(pdu);
			return EncodePduForTransmission(pdu);
		}

		/// <summary>
		/// Fills the specified pdu with data from instance's properties.
		/// </summary>
		/// <param name="pdu">The pdu.</param>
		protected abstract void AppendPduData(ArrayList pdu);

		/// <summary>
		/// Takes the given Pdu, calculates its length(trimming it beforehand), inserting
		/// its length, and copying it to a byte array.
		/// </summary>
		/// <param name="pdu">The Pdu to encode.</param>
		/// <returns>The byte array representation of the Pdu.</returns>
		protected byte[] EncodePduForTransmission(ArrayList pdu)
		{
			AddTlvBytes(ref pdu);
			pdu = InsertLengthIntoPdu(pdu);
			byte[] result = new byte[pdu.Count];
			pdu.CopyTo(result);

			return result;
		}

		/// <summary>
		/// Decodes the bind response from the SMSC.  This version throws a NotImplementedException.
		/// </summary>
		protected abstract void DecodeSmscResponse();
		
		#endregion overridable methods

		#region Utility methods
		/// <summary>
		/// Calculates the length of the given ArrayList representation of the Pdu and
		/// inserts this length into the appropriate spot in the Pdu.  This will call
		/// TrimToSize()on the ArrayList-the caller need not do it.
		/// </summary>
		/// <param name="pdu">The protocol data unit to calculate the
		/// length for.</param>
		/// <returns>The Pdu with the length inserted, trimmed to size.</returns>
		protected static ArrayList InsertLengthIntoPdu(ArrayList pdu)
		{
			pdu.TrimToSize();
			uint commandLength =(uint)(4 + pdu.Count);
			uint reqLenH2N = UnsignedNumConverter.SwapByteOrdering(commandLength);
			byte[] reqLenArray = BitConverter.GetBytes(reqLenH2N);
			//insert into the Pdu
			pdu.InsertRange(0, reqLenArray);
			pdu.TrimToSize();
			
			return pdu;
		}				
		
		/// <summary>
		/// What remains after the header is stripped off the Pdu.  Subclasses
		/// don't need the header as its information is stored here.  In
		/// addition, this allows them to manipulate the response data
		/// all they want without destroying the original.  The copying is
		/// done every time this property is accessed, so use caution in a
		/// high-performance setting.
		/// </summary>
		protected byte[] BytesAfterHeader
		{
			get
			{
				long length = _PacketBytes.Length - HEADER_LENGTH;
				byte[] remainder = new byte[length];
				Array.Copy(_PacketBytes, HEADER_LENGTH, remainder, 0, length);
				return remainder;
			}
		}
		
		/// <summary>
		/// Creates an ArrayList consisting of the Pdu header.  Command ID and status
		/// need to be set before calling this.
		/// </summary>
		/// <returns>The Pdu as a trimmed ArrayList.</returns>
		protected ArrayList GetPduHeader()
		{
			ArrayList pdu = new ArrayList();
			pdu.AddRange(BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering((uint)_CommandID)));
			pdu.AddRange(BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering((uint)_CommandStatus)));
			pdu.AddRange(BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering(_SequenceNumber)));
			pdu.TrimToSize();
			return pdu;
		}
		#endregion

		#region TLV table methods
		/// <summary>
		/// Determines whether [contains optional parameter] [specified by tag].
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>
		/// 	<c>true</c> if [contains optional parameter] [specified by tag]; otherwise, <c>false</c>.
		/// </returns>
		public bool ContainsOptionalParameter(OptionalParamCodes tag)
		{
			return _tlvTable.ContainsKey(UnsignedNumConverter.SwapByteOrdering((ushort)tag));
		}

		/// <summary>
		/// Gets the optional parameter string associated with
		/// the given tag.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <returns>The optional parameter string, or null if not found.</returns>
		public string GetOptionalParamString(OptionalParamCodes tag)
		{
			//return _tlvTable.GetOptionalParamString(UnsignedNumConverter.SwapByteOrdering(tag));

			if (!this.ContainsOptionalParameter(tag))
				return null;

			var bytes = _tlvTable.GetBytes(UnsignedNumConverter.SwapByteOrdering((ushort)tag));

			// Remove null termination (if found)
			if (bytes.Length > 0 && bytes[bytes.Length - 1] == 0x0)
				Array.Resize(ref bytes, bytes.Length - 1);

			return Encoding.ASCII.GetString(bytes);
		}
		/// <summary>
		/// Gets the optional parameter bytes associated with
		/// the given tag.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <returns>The optional parameter bytes, or null if not found</returns>
		public byte[] GetOptionalParamBytes(OptionalParamCodes tag)
		{
			//return _tlvTable.GetOptionalParamBytes(UnsignedNumConverter.SwapByteOrdering(tag));
			return this.ContainsOptionalParameter(tag) ?
				_tlvTable.GetBytes(UnsignedNumConverter.SwapByteOrdering((ushort)tag)) : null;
		}
		/// <summary>
		/// Gets the optional parameter of type T associated with
		/// the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>The optional parameter value, or null if not found</returns>
		public byte? GetOptionalParamByte(OptionalParamCodes tag)
		{
			return this.GetOptionalParamByte<byte>(tag);
		}
		/// <summary>
		/// Gets the optional parameter of type T associated with
		/// the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>The optional parameter value, or null if not found</returns>
		public T? GetOptionalParamByte<T>(OptionalParamCodes tag)
			where T : struct
		{
			if (!this.ContainsOptionalParameter(tag))
				return null;

			var data = _tlvTable.GetByte(UnsignedNumConverter.SwapByteOrdering((ushort)tag));
			
			return typeof(T).IsEnum ? (T)Enum.ToObject(typeof(T), data) : (T)Convert.ChangeType(data, typeof(T));
		}
		/// <summary>
		/// Retrieves the given bytes from the TLV table and converts them into a
		/// host order UInt16.
		/// </summary>
		/// <param name="tag">The TLV tag to use for retrieval</param>
		/// <returns>The host order result.</returns>
		protected UInt16? GetHostOrderUInt16FromTlv(OptionalParamCodes tag)
		{
			var data = GetOptionalParamBytes(tag);

			return data == null ? new Nullable<UInt16>() :
				UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt16(data, 0));
		}
		/// <summary>
		/// Retrieves the given bytes from the TLV table and converts them into a
		/// host order UInt32.
		/// </summary>
		/// <param name="tag">The TLV tag to use for retrieval</param>
		/// <returns>The host order result.</returns>
		protected UInt32? GetHostOrderUInt32FromTlv(OptionalParamCodes tag)
		{
			var data = GetOptionalParamBytes(tag);

			return data == null ? new Nullable<UInt32>() :
				UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt32(data, 0));
		}

		/// <summary>
		/// Sets the given TLV(as a string)into the table.
		/// This will reverse the byte order in the tag for you (necessary for encoding).
		/// If the value is null, the parameter TLV will be removed instead.
		/// </summary>
		/// <param name="tag">The tag for this TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void SetOptionalParamString(OptionalParamCodes tag, string val, bool nullTerminated)
		{
			if (val == null)
			{
				if (this.ContainsOptionalParameter(tag))
					this.RemoveOptionalParameter(tag);
			}
			else
			{
				var bytes = Encoding.ASCII.GetBytes(val);

				// Add a null byte to the end if needed.
				if (nullTerminated) Array.Resize(ref bytes, bytes.Length + 1);
				
				_tlvTable.Set(UnsignedNumConverter.SwapByteOrdering((ushort)tag), bytes);
			}
		}
		/// <summary>
		/// Sets the given TLV(as a byte) into the table.  This will not take
		/// care of big-endian/little-endian issues, although it will reverse the byte order 
		/// in the tag for you (necessary for encoding). 
		/// If the value is null, the parameter TLV will be removed instead.
		/// </summary>
		/// <param name="tag">The tag for this TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		/*public void SetOptionalParamByte(OptionalParamCodes tag, byte? val)
		{
			if (!val.HasValue)
			{
				if (this.ContainsOptionalParameter(tag))
					this.RemoveOptionalParameter(tag);
			}
			else
			{
				_tlvTable.Set(UnsignedNumConverter.SwapByteOrdering((ushort)tag), val.Value);
			}
		}*/
		/// <summary>
		/// Sets the given TLV(as a byte) into the table.  This will not take
		/// care of big-endian/little-endian issues, although it will reverse the byte order 
		/// in the tag for you (necessary for encoding). 
		/// If the value is null, the parameter TLV will be removed instead.
		/// </summary>
		/// <param name="tag">The tag for this TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void SetOptionalParamByte<T>(OptionalParamCodes tag, T? val)
			where T : struct
		{
			if (!val.HasValue)
			{
				if (this.ContainsOptionalParameter(tag))
					this.RemoveOptionalParameter(tag);
			}
			else
			{
				_tlvTable.Set(UnsignedNumConverter.SwapByteOrdering((ushort)tag), Convert.ToByte(val.Value));
			}
		}
		/// <summary>
		/// Sets the given TLV(as a byte array)into the table.  This will not take
		/// care of big-endian/little-endian issues, although it will reverse the byte order 
		/// in the tag for you(necessary for encoding).
		/// If the value is null, the parameter TLV will be removed instead.
		/// </summary>
		/// <param name="tag">The tag for this TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void SetOptionalParamBytes(OptionalParamCodes tag, byte[] val)
		{
			if (val == null)
			{
				if (this.ContainsOptionalParameter(tag))
					this.RemoveOptionalParameter(tag);
			}
			else
			{
				_tlvTable.Set(UnsignedNumConverter.SwapByteOrdering((ushort)tag), val);
			}
		}
		/// <summary>
		/// Takes the given value and puts it into the TLV table, accounting for 
		/// network byte ordering.
		/// </summary>
		/// <param name="tag">The TLV tag to use for retrieval</param>
		/// <param name="val">The value to put into the table</param>
		protected void SetHostOrderValueIntoTlv(OptionalParamCodes tag, UInt16? val)
		{
			if (!val.HasValue)
			{
				if (this.ContainsOptionalParameter(tag))
					RemoveOptionalParameter(tag);
			}
			else
			{
				if (val.Value >= UInt16.MaxValue) {
					var msg = string.Format("Value too large for ushort TLV '{0}'", tag);
					throw new ArgumentOutOfRangeException(msg);
				}

				SetOptionalParamBytes(tag, BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering(val.Value)));
			}
		}
		/// <summary>
		/// Takes the given value and puts it into the TLV table, accounting for 
		/// network byte ordering.
		/// </summary>
		/// <param name="tag">The TLV tag to use for retrieval</param>
		/// <param name="val">The value to put into the table</param>
		protected void SetHostOrderValueIntoTlv(OptionalParamCodes tag, UInt32? val)
		{
			if (!val.HasValue)
			{
				if (this.ContainsOptionalParameter(tag))
					RemoveOptionalParameter(tag);
			}
			else
			{
				if (val.Value < UInt32.MaxValue)
				{
					var msg = string.Format("Value too large for uint TLV '{0}'", tag);
					throw new ArgumentOutOfRangeException(msg);
				}

				SetOptionalParamBytes(tag, BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering(val.Value)));
			}
		}

		/// <summary>
		/// Removes the optional parameter.
		/// </summary>
		/// <param name="tag">The tag.</param>
		public void RemoveOptionalParameter(OptionalParamCodes tag)
		{
			_tlvTable.Remove(UnsignedNumConverter.SwapByteOrdering((ushort)tag));
		}

		/// <summary>
		/// Takes the given bytes and attempts to insert them into the TLV table.
		/// </summary>
		/// <param name="tlvBytes">The bytes to convert for the TLVs.</param>
		protected void TranslateTlvDataIntoTable(byte[] tlvBytes)
		{
			_tlvTable.TranslateTlvDataIntoTable(tlvBytes);
		}
		
		/// <summary>
		/// Takes the given bytes and attempts to insert them into the TLV table.
		/// </summary>
		/// <param name="tlvBytes">The bytes to convert for the TLVs.</param>
		/// <param name="index">The index of the byte array to start at.  This is here
		/// because in some instances you may not want to start at the
		/// beginning.</param>
		protected void TranslateTlvDataIntoTable(byte[] tlvBytes, Int32 index)
		{
			#if DEBUG
			Console.WriteLine("Calling TranslateTlvDataIntoTable(byte[], Int32)");
			#endif
			_tlvTable.TranslateTlvDataIntoTable(tlvBytes, index);
		}
		
		/// <summary>
		/// Adds the TLV bytes to the given Pdu.
		/// </summary>
		/// <param name="pdu">The Pdu to add to.</param>
		protected void AddTlvBytes(ref ArrayList pdu)
		{
			ArrayList tlvs = _tlvTable.GenerateByteEncodedTlv();
			foreach(byte[] tlv in tlvs)
			{
				#if DEBUG
				StringBuilder sb = new StringBuilder("\nAdding TLV bytes\n");
				for(int i = 0; i < tlv.Length; i++) 
				{
					sb.Append(tlv[i].ToString("X").PadLeft(2, '0'));
					sb.Append(" ");
				}
				sb.Append("\n");
				Console.WriteLine(sb);
				
				#endif
				
				pdu.AddRange(tlv);
			}
		}
		
		#endregion TLV table methods
		
		#region enumerations
		
		/// <summary>
		/// Enumerates the bearer types for source and destination.
		/// </summary>
		public enum BearerType : byte
		{
			/// <summary>
			/// Unknown
			/// </summary>
			Unknown = 0x00,
			/// <summary>
			/// SMS
			/// </summary>
			SMS = 0x01,
			/// <summary>
			/// CircuitSwitchedData
			/// </summary>
			CircuitSwitchedData = 0x02,
			/// <summary>
			/// PacketData
			/// </summary>
			PacketData = 0x03,
			/// <summary>
			/// USSD
			/// </summary>
			USSD = 0x04,
			/// <summary>
			/// CDPD
			/// </summary>
			CDPD = 0x05,
			/// <summary>
			/// DataTAC
			/// </summary>
			DataTAC = 0x06,
			/// <summary>
			/// FLEX_ReFLEX
			/// </summary>
			FLEX_ReFLEX = 0x07,
			/// <summary>
			/// CellBroadcast
			/// </summary>
			CellBroadcast = 0x08
		}
		
		/// <summary>
		/// Enumerates the network types for the source and destination.
		/// </summary>
		public enum NetworkType : byte
		{
			/// <summary>
			/// Unknown
			/// </summary>
			Unknown = 0x00,
			/// <summary>
			/// GSM
			/// </summary>
			GSM = 0x01,
			/// <summary>
			/// ANSI_136_TDMA
			/// </summary>
			ANSI_136_TDMA = 0x02,
			/// <summary>
			/// IS_95_CDMA
			/// </summary>
			IS_95_CDMA = 0x03,
			/// <summary>
			/// PDC
			/// </summary>
			PDC = 0x04,
			/// <summary>
			/// PHS
			/// </summary>
			PHS = 0x05,
			/// <summary>
			/// iDEN
			/// </summary>
			iDEN = 0x06,
			/// <summary>
			/// AMPS
			/// </summary>
			AMPS = 0x07,
			/// <summary>
			/// PagingNetwork
			/// </summary>
			PagingNetwork = 0x08
		}
		
		/// <summary>
		/// Enumerates the different states a message can be in.
		/// </summary>
		public enum MessageStateType : byte
		{
			/// <summary>
			/// Enroute
			/// </summary>
			Enroute = 1,
			/// <summary>
			/// Delivered
			/// </summary>
			Delivered = 2,
			/// <summary>
			/// Expired
			/// </summary>
			Expired = 3,
			/// <summary>
			/// Deleted
			/// </summary>
			Deleted = 4,
			/// <summary>
			/// Undeliverable
			/// </summary>
			Undeliverable = 5,
			/// <summary>
			/// Accepted
			/// </summary>
			Accepted = 6,
			/// <summary>
			/// Unknown
			/// </summary>
			Unknown = 7,
			/// <summary>
			/// Rejected
			/// </summary>
			Rejected = 8
		}
		
		/// <summary>
		/// SMPP version type.
		/// </summary>
		public enum SmppVersionType : byte
		{
			/// <summary>
			/// Version 3.3 of the SMPP spec.
			/// </summary>
			Version3_3 = 0x33,
			/// <summary>
			/// Version 3.4 of the SMPP spec.
			/// </summary>
			Version3_4 = 0x34
		}
		
		/// <summary>
		/// Enumerates the source address subunit types.
		/// </summary>
		public enum AddressSubunitType : byte
		{
			/// <summary>
			/// Unknown
			/// </summary>
			Unknown = 0x00,
			/// <summary>
			/// MSDisplay
			/// </summary>
			MSDisplay = 0x01,
			/// <summary>
			/// MobileEquipment
			/// </summary>
			MobileEquipment = 0x02,
			/// <summary>
			/// SmartCard1
			/// </summary>
			SmartCard1 = 0x03,
			/// <summary>
			/// ExternalUnit1
			/// </summary>
			ExternalUnit1 = 0x04
		}
		
		/// <summary>
		/// Enumerates the display time type.
		/// </summary>
		public enum DisplayTimeType : byte
		{
			/// <summary>
			/// Temporary
			/// </summary>
			Temporary = 0x00,
			/// <summary>
			/// Default
			/// </summary>
			Default = 0x01,
			/// <summary>
			/// Invoke
			/// </summary>
			Invoke = 0x02
		}
		
		/// <summary>
		/// Enumerates the type of the ITS Reply Type
		/// </summary>
		public enum ItsReplyTypeType : byte
		{
			/// <summary>
			/// Digit
			/// </summary>
			Digit = 0,
			/// <summary>
			/// Number
			/// </summary>
			Number = 1,
			/// <summary>
			/// TelephoneNum
			/// </summary>
			TelephoneNum = 2,
			/// <summary>
			/// Password
			/// </summary>
			Password = 3,
			/// <summary>
			/// CharacterLine
			/// </summary>
			CharacterLine = 4,
			/// <summary>
			/// Menu
			/// </summary>
			Menu = 5,
			/// <summary>
			/// Date
			/// </summary>
			Date = 6,
			/// <summary>
			/// Time
			/// </summary>
			Time = 7,
			/// <summary>
			/// Continue
			/// </summary>
			Continue = 8
		}
		
		/// <summary>
		/// Enumerates the MS Display type.
		/// </summary>
		public enum MsValidityType : byte
		{
			/// <summary>
			/// StoreIndefinitely
			/// </summary>
			StoreIndefinitely = 0x00,
			/// <summary>
			/// PowerDown
			/// </summary>
			PowerDown = 0x01,
			/// <summary>
			/// SIDBased
			/// </summary>
			SIDBased = 0x02,
			/// <summary>
			/// DisplayOnly
			/// </summary>
			DisplayOnly = 0x03
		}
				
		/// <summary>
		/// Enumerates the type of number types that can be used for the SMSC 
		/// message
		/// sending.
		/// </summary>
		public enum TonType : byte
		{
			/// <summary>
			/// Unknown
			/// </summary>
			Unknown = 0x00,
			/// <summary>
			/// International (E.164)
			/// </summary>
			International = 0x01,
			/// <summary>
			/// National
			/// </summary>
			National = 0x02,
			/// <summary>
			/// Network specific
			/// </summary>
			NetworkSpecific = 0x03,
			/// <summary>
			/// Subscriber number
			/// </summary>
			SubscriberNumber = 0x04,
			/// <summary>
			/// Alphanumeric
			/// </summary>
			Alphanumeric = 0x05,
			/// <summary>
			/// Abbreviated
			/// </summary>
			Abbreviated = 0x06
		}
		
		/// <summary>
		/// Enumerates the number plan indicator types that can be used for the 
		/// SMSC
		/// message sending.
		/// </summary>
		public enum NpiType : byte
		{
			/// <summary>
			/// Unknown
			/// </summary>
			Unknown = 0x00,
			/// <summary>
			/// ISDN
			/// </summary>
			ISDN = 0x01,
			/// <summary>
			/// Data
			/// </summary>
			Data = 0x03,
			/// <summary>
			/// Telex
			/// </summary>
			Telex = 0x04,
			/// <summary>
			/// Land mobile
			/// </summary>
			LandMobile = 0x06,
			/// <summary>
			/// National
			/// </summary>
			National = 0x08,
			/// <summary>
			/// Private
			/// </summary>
			Private = 0x09,
			/// <summary>
			/// ERMES
			/// </summary>
			ERMES = 0x0A,
			/// <summary>
			/// Internet
			/// </summary>
			Internet = 0x0E
		}
		
		/// <summary>
		/// Enumerates the priority level of the message.
		/// </summary>
		public enum PriorityType : byte
		{
			/// <summary>
			/// Lowest
			/// </summary>
			Lowest = 0x00,
			/// <summary>
			/// Level1
			/// </summary>
			Level1 = 0x01,
			/// <summary>
			/// Level2
			/// </summary>
			Level2 = 0x02,
			/// <summary>
			/// Highest
			/// </summary>
			Highest = 0x03
		}
		
		/// <summary>
		/// Enumerates the types of registered delivery.  Not all possible options 
		/// are present, just the common ones.
		/// </summary>
		public enum RegisteredDeliveryType : byte
		{
			/// <summary>
			/// No registered delivery
			/// </summary>
			None = 0x00,
			/// <summary>
			/// Notification on success or failure
			/// </summary>
			OnSuccessOrFailure = 0x01,
			/// <summary>
			/// Notification on failure only
			/// </summary>
			OnFailure = 0x02
		}
				
		/// <summary>
		/// Enumerates the privacy indicator types.
		/// </summary>
		public enum PrivacyType : byte
		{
			/// <summary>
			/// Nonrestricted
			/// </summary>
			Nonrestricted = 0x00,
			/// <summary>
			/// Restricted
			/// </summary>
			Restricted = 0x01,
			/// <summary>
			/// Confidential
			/// </summary>
			Confidential = 0x03,
			/// <summary>
			/// Secret
			/// </summary>
			Secret = 0x03
		}
		
		/// <summary>
		/// Enumerates the types of payload type.
		/// </summary>
		public enum PayloadTypeType : byte
		{
			/// <summary>
			/// WDPMessage
			/// </summary>
			WDPMessage = 0x00,
			/// <summary>
			/// WCMPMessage
			/// </summary>
			WCMPMessage = 0x01
		}
		
		/// <summary>
		/// Enumerates the DPF result types.
		/// </summary>
		public enum DpfResultType : byte
		{
			/// <summary>
			/// DPFNotSet
			/// </summary>
			DPFNotSet = 0,
			/// <summary>
			/// DPFSet
			/// </summary>
			DPFSet = 1
		}		
		
		#endregion enumerations
		
		#region utility methods

		/// <summary>
		/// Utility method to allow the Pdu factory to decode the command
		/// ID without knowing about packet structure.  Some SMSCs combine
		/// response packets(even though they shouldn't).
		/// </summary>
		/// <param name="response">The Pdu response packet.</param>
		/// <returns>The ID of the Pdu command(e.g. cancel_sm_resp).</returns>
		public static CommandId DecodeCommandId(byte[] response)
		{
			uint id = 0;
			try
			{
				id = UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt32(response, 4));
				return(CommandId)id;
			}
			catch		//possible that we are reading a bad command
			{
				return CommandId.generic_nack;
			}
		}
		
		/// <summary>
		/// Utility method to allow the Pdu factory to decode the command
		/// length without knowing about packet structure.  Some SMSCs combine
		/// response packets(even though they shouldn't).
		/// </summary>
		/// <param name="response">The Pdu response packet.</param>
		/// <returns>The length of the Pdu command.</returns>
		public static UInt32 DecodeCommandLength(byte[] response)
		{
			return UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt32(response, 0));
		}
		
		#endregion utility methods
		
		#region IDisposable methods
		
		/// <summary>
		/// Implementation of IDisposable
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this); 
		}
	
		/// <summary>
		/// Method for derived classes to implement.
		/// </summary>
		/// <param name="disposing">Set to false if called from a finalizer.</param>
		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				// Free other state(managed objects).
			}
			// Free your own state(unmanaged objects).
			// Set large fields to null.
		}
	
		/// <summary>
		/// Finalizer.  Base classes will inherit this-used when Dispose()is not automatically called.
		/// </summary>
		~Pdu()
		{
			// Simply call Dispose(false).
			Dispose(false);
		}
		
		#endregion IDisposable methods

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("{0} -- ", GetType().Name);

			foreach (var property in GetType().GetProperties())
			{
				object value = "--";

				try { 
					value = property.GetValue(this, null);
				} catch { }

				if (value is byte[])
				{
					var ba = value as byte[];
					var hex = new StringBuilder(ba.Length * 2);
					foreach (byte b in ba) hex.AppendFormat("{0:x2}", b);
					value = hex.ToString();
				}

				sb.AppendFormat("{0}:{1} ", property.Name, value);
			}

			return sb.ToString();
		}
	}
}
