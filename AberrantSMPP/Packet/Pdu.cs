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
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections;
using System.Diagnostics;
using AberrantSMPP.Utility;
using System.Text;

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
		
		private static uint _StaticSequenceNumber = 0;
		private uint _CommandStatus;
		private CommandIdType _CommandID;
		private TlvTable _tlvTable = new TlvTable();
		private uint _CustomSequenceNumber = 0;
		private uint _SequenceNumber = 0;
		
		private byte[] _PacketBytes = new byte[0];
		private uint _CommandLength;
		
		#endregion private fields
		
		#region properties
		
		/// <summary>
		/// Gets or sets the byte response from the SMSC.  This will return a clone of the 
		/// byte array upon a "get" request. 
		/// </summary>
		public byte[] PacketBytes
		{
			get
			{
				return(byte[])_PacketBytes.Clone();
			}
			
			set
			{
				_PacketBytes = value;
			}
		}
		
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
				_CustomSequenceNumber = value;
			}
		}
		
		/// <summary>
		/// Indicates outcome of request.
		/// </summary>
		public uint CommandStatus
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
		protected CommandIdType CommandID
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
		
		#endregion properties
		
		#region constructors
		
		/// <summary>
		/// Constructor for sent Pdus.
		/// </summary>
		protected Pdu()
		{
			InitPdu();
		}
		
		/// <summary>
		/// Constructor for received Pdus.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to translate to a Pdu.</param>
		protected Pdu(byte[] incomingBytes)
		{
			#if DEBUG
			Console.WriteLine("In Pdu byte[] constructor");
			#endif
			_PacketBytes = incomingBytes;
			_CommandLength = DecodeCommandLength(_PacketBytes);
			_CommandID = DecodeCommandId(_PacketBytes);
			_CommandStatus = UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt32(_PacketBytes, 8));
			_SequenceNumber = UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt32(_PacketBytes, 12));
			_PacketBytes = TrimResponsePdu(_PacketBytes);
			
			//set the other Pdu-specific fields
			DecodeSmscResponse();
		}
		
		#endregion constructors
		
		#region overridable methods
		
		/// <summary>
		/// Initializes this Pdu.  Override to add more functionality, but don't forget to 
		/// call this one.
		/// </summary>
		protected virtual void InitPdu()
		{
			CommandStatus = 0;
			CommandID = Pdu.CommandIdType.generic_nack;
		}
		
		///<summary>
		/// Gets the hex encoding(big-endian)of this Pdu.
		///</summary>
		///<return>The hex-encoded version of the Pdu</return>
		public abstract void ToMsbHexEncoding();
		/*public virtual void ToMsbHexEncoding()
		{
			throw new NotImplementedException("GetMSBHexEncoding is not implemented in Pdu.");
		}*/
		
		/// <summary>
		/// Decodes the bind response from the SMSC.  This version throws a NotImplementedException.
		/// </summary>
		protected abstract void DecodeSmscResponse();
		/*protected virtual void DecodeSmscResponse()
		{
			throw new NotImplementedException("DecodeSmscResponse is not implemented in Pdu.");
		}*/
		
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
		/// Retrieves the given bytes from the TLV table and converts them into a
		/// host order UInt16.
		/// </summary>
		/// <param name="tag">The TLV tag to use for retrieval</param>
		/// <returns>The host order result.</returns>
		protected UInt16 GetHostOrderUInt16FromTlv(ushort tag)
		{
			return UnsignedNumConverter.SwapByteOrdering(
				BitConverter.ToUInt16(GetOptionalParamBytes(tag), 0));
		}
		
		/// <summary>
		/// Retrieves the given bytes from the TLV table and converts them into a
		/// host order UInt32.
		/// </summary>
		/// <param name="tag">The TLV tag to use for retrieval</param>
		/// <returns>The host order result.</returns>
		protected UInt32 GetHostOrderUInt32FromTlv(ushort tag)
		{
			return UnsignedNumConverter.SwapByteOrdering(
				BitConverter.ToUInt32(GetOptionalParamBytes(tag), 0));
		}
		
		/// <summary>
		/// Takes the given value and puts it into the TLV table, accounting for 
		/// network byte ordering.
		/// </summary>
		/// <param name="tag">The TLV tag to use for retrieval</param>
		/// <param name="val">The value to put into the table</param>
		protected void SetHostOrderValueIntoTlv(UInt16 tag, UInt16 val)
		{
			SetOptionalParamBytes(
						tag,
						BitConverter.GetBytes(
						UnsignedNumConverter.SwapByteOrdering(val)));
		}
		
		/// <summary>
		/// Takes the given value and puts it into the TLV table, accounting for 
		/// network byte ordering.
		/// </summary>
		/// <param name="tag">The TLV tag to use for retrieval</param>
		/// <param name="val">The value to put into the table</param>
		protected void SetHostOrderValueIntoTlv(UInt16 tag, UInt32 val)
		{
			SetOptionalParamBytes(
						tag,
						BitConverter.GetBytes(
						UnsignedNumConverter.SwapByteOrdering(val)));
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
			pdu.AddRange(BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering(_CommandStatus)));
			
			//increase the sequence number
			GenerateSequenceNumber();			
			pdu.AddRange(BitConverter.GetBytes(UnsignedNumConverter.SwapByteOrdering(_SequenceNumber)));
			pdu.TrimToSize();
			return pdu;
		}
		
		/// <summary>
		/// Generates a monotonically increasing sequence number for each Pdu.  When it
		/// hits the the 32 bit unsigned int maximum, it starts over.
		/// </summary>
		private void GenerateSequenceNumber()
		{
			if(_CustomSequenceNumber == 0)
			{
				_StaticSequenceNumber++;
				if(_StaticSequenceNumber >= UInt32.MaxValue)
				{
					_StaticSequenceNumber = 1;
				}
				_SequenceNumber = _StaticSequenceNumber;
			}
			else
			{
				_SequenceNumber = _CustomSequenceNumber;
			}
		}

		#endregion

		#region TLV table methods

		/// <summary>
		/// Gets the optional parameter string associated with
		/// the given tag.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <returns>The optional parameter string, the empty
		/// string if not found.</returns>
		public string GetOptionalParamString(UInt16 tag)
		{
			return _tlvTable.GetOptionalParamString(UnsignedNumConverter.SwapByteOrdering(tag));
		}
		
		/// <summary>
		/// Gets the optional parameter bytes associated with
		/// the given tag.
		/// </summary>
		/// <param name="tag">The tag in TLV.</param>
		/// <returns>The optional parameter bytes, null if
		/// not found.</returns>
		public byte[] GetOptionalParamBytes(UInt16 tag)
		{
			return _tlvTable.GetOptionalParamBytes(UnsignedNumConverter.SwapByteOrdering(tag));
		}
		
		/// <summary>
		/// Sets the given TLV(as a string)into the table.  This ignores
		/// null values.  This will reverse the byte order in the tag for you 
		///(necessary for encoding).
		/// </summary>
		/// <param name="tag">The tag for this TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void SetOptionalParamString(UInt16 tag, string val)
		{
			_tlvTable.SetOptionalParamString(UnsignedNumConverter.SwapByteOrdering(tag), val);
		}
		
		/// <summary>
		/// Sets the given TLV(as a byte array)into the table.  This will not take
		/// care of big-endian/little-endian issues, although it will reverse the byte order 
		/// in the tag for you(necessary for encoding).  This ignores null values.
		/// </summary>
		/// <param name="tag">The tag for this TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void SetOptionalParamBytes(UInt16 tag, byte[] val)
		{
			_tlvTable.SetOptionalParamBytes(UnsignedNumConverter.SwapByteOrdering(tag), val);
		}
		
		/// <summary>
		/// Allows the updating of TLV values.  This will not take care of 
		/// big-endian/little-endian issues, although it will reverse the byte order 
		/// in the tag for you(necessary for encoding).
		/// </summary>
		/// <param name="tag">The tag for this TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void UpdateOptionalParamBytes(UInt16 tag, byte[] val)
		{
			_tlvTable.UpdateOptionalParamBytes(UnsignedNumConverter.SwapByteOrdering(tag), val);
		}
		
		/// <summary>
		/// Allows the updating of TLV values.  This will reverse the byte order in the tag for you 
		///(necessary for encoding).
		/// </summary>
		/// <param name="tag">The tag for this TLV.</param>
		/// <param name="val">The value of this TLV.</param>
		public void UpdateOptionalParamString(UInt16 tag, string val)
		{
			_tlvTable.UpdateOptionalParamString(UnsignedNumConverter.SwapByteOrdering(tag), val);
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
		/// Enumerates all of the "standard" optional codes.  This is more for
		/// convenience when writing/updating this library than for end programmers,
		/// as the the TLV table methods take a ushort/UInt16 rather than an
		/// OptionalParamCodes enumeration.
		/// </summary>
		public enum OptionalParamCodes : ushort
		{
			/// <summary>
			/// Destination address subunit
			/// </summary>
			dest_addr_subunit = 0x0005,
			/// <summary>
			/// Destination address network type
			/// </summary>
			dest_network_type = 0x0006,
			/// <summary>
			/// Destination address bearer type
			/// </summary>
			dest_bearer_type = 0x0007,
			/// <summary>
			/// Destination address telematics ID
			/// </summary>
			dest_telematics_id = 0x0008,
			/// <summary>
			/// Source address subunit
			/// </summary>
			source_addr_subunit = 0x000D,
			/// <summary>
			/// Source address network type
			/// </summary>
			source_network_type = 0x000E,
			/// <summary>
			/// Source address bearer type
			/// </summary>
			source_bearer_type = 0x000F,
			/// <summary>
			/// Source address telematics ID
			/// </summary>
			source_telematics_id = 0x0010,
			/// <summary>
			/// Quality of service time to live
			/// </summary>
			qos_time_to_live = 0x0017,
			/// <summary>
			/// Payload type
			/// </summary>
			payload_type = 0x0019,
			/// <summary>
			/// Additional status info
			/// </summary>
			additional_status_info_text = 0x001D,
			/// <summary>
			/// Receipted message ID
			/// </summary>
			receipted_message_id = 0x001E,
			/// <summary>
			/// Message wait facilities
			/// </summary>
			ms_msg_wait_facilities = 0x0030,
			/// <summary>
			/// Privacy indicator
			/// </summary>
			privacy_indicator = 0x0201,
			/// <summary>
			/// Source subaddress
			/// </summary>
			source_subaddress = 0x0202,
			/// <summary>
			/// Destination subaddress
			/// </summary>
			dest_subaddress = 0x0203,
			/// <summary>
			/// User message reference
			/// </summary>
			user_message_reference = 0x0204,
			/// <summary>
			/// User response code
			/// </summary>
			user_response_code = 0x0205,
			/// <summary>
			/// Source port
			/// </summary>
			source_port = 0x020A,
			/// <summary>
			/// Destination port
			/// </summary>
			destination_port = 0x020B,
			/// <summary>
			/// Message reference number
			/// </summary>
			sar_msg_ref_num = 0x020C,
			/// <summary>
			/// Language indicator
			/// </summary>
			language_indicator = 0x020D,
			/// <summary>
			/// Total segments
			/// </summary>
			sar_total_segments = 0x020E,
			/// <summary>
			/// Segment sequence number
			/// </summary>
			sar_segment_seqnum = 0x020F,
			/// <summary>
			/// Interface version
			/// </summary>
			SC_interface_version = 0x0210,
			/// <summary>
			/// Callback number indicator
			/// </summary>
			callback_num_pres_ind = 0x0302,
			/// <summary>
			/// Callback number tag
			/// </summary>
			callback_num_atag = 0x0303,
			/// <summary>
			/// Total number of messages
			/// </summary>
			number_of_messages = 0x0304,
			/// <summary>
			/// Callback number
			/// </summary>
			callback_num = 0x0381,
			/// <summary>
			/// DPF result
			/// </summary>
			dpf_result = 0x0420,
			/// <summary>
			/// Set DPF
			/// </summary>
			set_dpf = 0x0421,
			/// <summary>
			/// Availability status
			/// </summary>
			ms_availability_status = 0x0422,
			/// <summary>
			/// Network error code
			/// </summary>
			network_error_code = 0x0423,
			/// <summary>
			/// Message payload
			/// </summary>
			message_payload = 0x0424,
			/// <summary>
			/// Reason for delivery failure
			/// </summary>
			delivery_failure_reason = 0x0425,
			/// <summary>
			/// More messages to send flag
			/// </summary>
			more_messages_to_send = 0x0426,
			/// <summary>
			/// Message state
			/// </summary>
			message_state = 0x0427,
			/// <summary>
			/// USSD service opcode
			/// </summary>
			ussd_service_op = 0x0501,
			/// <summary>
			/// Display time
			/// </summary>
			display_time = 0x1201,
			/// <summary>
			/// SMS signal
			/// </summary>
			sms_signal = 0x1203,
			/// <summary>
			/// Message validity
			/// </summary>
			ms_validity = 0x1204,
			/// <summary>
			/// Alert on message delivery
			/// </summary>
			alert_on_message_delivery = 0x130C,
			/// <summary>
			/// ITS reply type
			/// </summary>
			its_reply_type = 0x1380,
			/// <summary>
			/// ITS session info
			/// </summary>
			its_session_info = 0x1383
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
			/// International
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
		/// Enumerates the data coding types.
		/// </summary>
		public enum DataCodingType : byte
		{
			/// <summary>
			/// SMSCDefault
			/// </summary>
			SMSCDefault = 0x00,
			/// <summary>
			/// IA5_ASCII
			/// </summary>
			IA5_ASCII = 0x01,
			/// <summary>
			/// OctetUnspecifiedB
			/// </summary>
			OctetUnspecifiedB = 0x02,
			/// <summary>
			/// Latin1
			/// </summary>
			Latin1 = 0x03,
			/// <summary>
			/// OctetUnspecifiedA
			/// </summary>
			OctetUnspecifiedA = 0x04,
			/// <summary>
			/// JIS
			/// </summary>
			JIS = 0x05,
			/// <summary>
			/// Cyrillic
			/// </summary>
			Cyrillic = 0x06,
			/// <summary>
			/// Latin_Hebrew
			/// </summary>
			Latin_Hebrew = 0x07,
			/// <summary>
			/// Pictogram
			/// </summary>
			Pictogram = 0x09,
			/// <summary>
			/// MusicCodes
			/// </summary>
			MusicCodes = 0x0A,
			/// <summary>
			/// ExtendedKanjiJIS
			/// </summary>
			ExtendedKanjiJIS = 0x0D,
			/// <summary>
			/// KS_C
			/// </summary>
			KS_C = 0x0E
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
		/// Enumerates the language types.
		/// </summary>
		public enum LanguageType : byte
		{
			/// <summary>
			/// Unspecified
			/// </summary>
			Unspecified = 0x00,
			/// <summary>
			/// English
			/// </summary>
			English = 0x01,
			/// <summary>
			/// French
			/// </summary>
			French = 0x02,
			/// <summary>
			/// Spanish
			/// </summary>
			Spanish = 0x03,
			/// <summary>
			/// German
			/// </summary>
			German = 0x04,
			/// <summary>
			/// Portuguese
			/// </summary>
			Portuguese = 0x05
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
		
		/// <summary>
		/// Enumeration of all the Pdu command types.
		/// </summary>
		public enum CommandIdType : uint
		{
			/// <summary>
			/// generic_nack
			/// </summary>
			generic_nack = 0x80000000,
			/// <summary>
			/// bind_receiver
			/// </summary>
			bind_receiver = 0x00000001,
			/// <summary>
			/// bind_receiver_resp
			/// </summary>
			bind_receiver_resp = 0x80000001,
			/// <summary>
			/// bind_transmitter
			/// </summary>
			bind_transmitter = 0x00000002,
			/// <summary>
			/// bind_transmitter_resp
			/// </summary>
			bind_transmitter_resp = 0x80000002,
			/// <summary>
			/// query_sm
			/// </summary>
			query_sm = 0x00000003,
			/// <summary>
			/// query_sm_resp
			/// </summary>
			query_sm_resp = 0x80000003,
			/// <summary>
			/// submit_sm
			/// </summary>
			submit_sm = 0x00000004,
			/// <summary>
			/// submit_sm_resp
			/// </summary>
			submit_sm_resp = 0x80000004,
			/// <summary>
			/// deliver_sm
			/// </summary>
			deliver_sm = 0x00000005,
			/// <summary>
			/// deliver_sm_resp
			/// </summary>
			deliver_sm_resp = 0x80000005,
			/// <summary>
			/// unbind
			/// </summary>
			unbind = 0x00000006,
			/// <summary>
			/// unbind_resp
			/// </summary>
			unbind_resp = 0x80000006,
			/// <summary>
			/// replace_sm
			/// </summary>
			replace_sm = 0x00000007,
			/// <summary>
			/// replace_sm_resp
			/// </summary>
			replace_sm_resp = 0x80000007,
			/// <summary>
			/// cancel_sm
			/// </summary>
			cancel_sm = 0x00000008,
			/// <summary>
			/// cancel_sm_resp
			/// </summary>
			cancel_sm_resp = 0x80000008,
			/// <summary>
			/// bind_transceiver
			/// </summary>
			bind_transceiver = 0x00000009,
			/// <summary>
			/// bind_transceiver_resp
			/// </summary>
			bind_transceiver_resp = 0x80000009,
			/// <summary>
			/// outbind
			/// </summary>
			outbind = 0x0000000B,
			/// <summary>
			/// enquire_link
			/// </summary>
			enquire_link = 0x00000015,
			/// <summary>
			/// enquire_link_resp
			/// </summary>
			enquire_link_resp = 0x80000015,
			/// <summary>
			/// submit_multi
			/// </summary>
			submit_multi = 0x00000021,
			/// <summary>
			/// submit_multi_resp
			/// </summary>
			submit_multi_resp = 0x80000021,
			/// <summary>
			/// alert_notification
			/// </summary>
			alert_notification = 0x00000102,
			/// <summary>
			/// data_sm
			/// </summary>
			data_sm = 0x00000103,
			/// <summary>
			/// data_sm_resp
			/// </summary>
			data_sm_resp = 0x80000103
		}
		
		#endregion enumerations
		
		#region utility methods
		
		/// <summary>
		/// Trims the trailing zeroes off of the response Pdu.  Useful for
		/// tracing and other purposes.  This uses the command length to
		/// actually trim it down, so TLVs and strings are not lost.  If the
		/// response actually is the same length as the command length, this
		/// method performs a pass-through.
		/// </summary>
		/// <returns>The trimmed Pdu(byte array).</returns>
		public static byte[] TrimResponsePdu(byte[] response)
		{
			uint commLength = DecodeCommandLength(response);
			if(commLength == response.Length)
			{
				return response;
			}
			//trap any weird data coming in
			if(commLength >= Int32.MaxValue || commLength > response.Length)
			{
				return new Byte[0];
			}
			
			byte[] trimmed = new Byte[commLength];
			for(int i = 0; i < trimmed.Length; i++)
			{
				trimmed[i] = response[i];
			}
			
			return trimmed;
		}
		
		/// <summary>
		/// Utility method to allow the Pdu factory to decode the command
		/// ID without knowing about packet structure.  Some SMSCs combine
		/// response packets(even though they shouldn't).
		/// </summary>
		/// <param name="response">The Pdu response packet.</param>
		/// <returns>The ID of the Pdu command(e.g. cancel_sm_resp).</returns>
		public static CommandIdType DecodeCommandId(byte[] response)
		{
			uint id = 0;
			try
			{
				id = UnsignedNumConverter.SwapByteOrdering(BitConverter.ToUInt32(response, 4));
				return(CommandIdType)id;
			}
			catch		//possible that we are reading a bad command
			{
				return CommandIdType.generic_nack;
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

				sb.AppendFormat("{0}:{1} ", property.Name, value);
			}

			return sb.ToString();
		}
	}
}
