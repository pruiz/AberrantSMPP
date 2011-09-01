using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace AberrantSMPP.Utility
{
	public static class SmppUtil
	{
		/// <summary>
		/// Gets the maximum length of each segment of a concatenated 
		/// message of totalBytes size using the specified data_coding.
		/// </summary>
		/// <param name="coding">The coding.</param>
		/// <param name="totalbytes">The totalbytes.</param>
		/// <returns></returns>
		public static int GetMaxSegmentLength(DataCoding coding, int totalbytes)
		{
			switch (coding)
			{
				case DataCoding.IA5_ASCII:
				case DataCoding.SMSCDefault:
					return totalbytes <= 160 ? 160 : 153;
				case DataCoding.UCS2:
					//return totalbytes <= 70 ? 70 : 67;
				case DataCoding.Latin1:
				case DataCoding.OctetUnspecifiedA:
				case DataCoding.OctetUnspecifiedB:
				case DataCoding.Cyrillic:
				case DataCoding.ExtendedKanjiJIS:
				case DataCoding.JIS:
				case DataCoding.KS_C:
				case DataCoding.Latin_Hebrew:
				case DataCoding.MusicCodes:
				case DataCoding.Pictogram:
					return totalbytes <= 140 ? 140 : 134;
				default:
					throw new InvalidOperationException("Invalid or unsuported encoding for text message ");
			}
		}

		/// <summary>
		/// Splits the message into segments of at most maxLen bytes.
		/// If udhRef is not null, the appropiate User Data Header will be 
		/// prepended to each segment, using it's value as re-assembly reference id.
		/// </summary>
		/// <param name="bytes">The bytes.</param>
		/// <param name="maxLen">The max len.</param>
		/// <param name="udhRef">The udh ref.</param>
		/// <returns></returns>
		public static IEnumerable<byte[]> SplitMessage(byte[] bytes, int maxLen, byte? udhRef)
		{
			if (bytes.Length <= maxLen)
				return new[] { bytes };

			var totalSegments = (bytes.Length / maxLen);
			var segments = new List<byte[]>();

			for (var i = 0; i <= totalSegments; i++)
			{
				var len = i == totalSegments ? bytes.Length - (maxLen * i) : maxLen;
				var segment = new byte[udhRef.HasValue ? 6 + len : len];

				if (udhRef.HasValue)
				{
					segment[0] = 5;		// Header len (not counting this len indicator byte)
					segment[1] = 0x00;	// Segmentation & re-assemly (with 8 bit reference) IE
					segment[2] = 0x03;	// IE data length.
					segment[3] = udhRef.Value;
					segment[4] = Convert.ToByte(totalSegments + 1);
					segment[5] = Convert.ToByte(i + 1);
				}

				Array.Copy(bytes, maxLen * i, segment, udhRef.HasValue ? 6 : 0, len);
				segments.Add(segment);
			}

			return segments;
		}

		/// <summary>
		/// Apply segmentation over a message possibly splitting it on multiple SMPP PDUs.
		/// </summary>
		/// <remarks>
		/// Method may return the passed pdu (modified as needed) as result in case no splitting is required.
		/// </remarks>
		/// <param name="pdu">The base pdu to use.</param>
		/// <param name="method">The segmentation & reasembly method tu use when splitting the message.</param>
		/// <param name="correlationId">The correlation id to set to each message part.</param>
		/// <returns>The list of sequence numbers of PDUs sent</returns>
		public static IEnumerable<SmppSubmitSm> SplitLongMessage(SmppSubmitSm pdu, SmppSarMethod method, byte correlationId)
		{
			if (pdu == null) throw new ArgumentNullException("pdu");

			var result = new List<SmppSubmitSm>();
			var data = pdu.MessagePayload != null ? pdu.MessagePayload : pdu.ShortMessage;

			if (data != null && !(data is string || data is byte[]))
				throw new ArgumentException("Short Message must be a string or byte array.");

			var bytes = data is string ? PduUtil.GetEncodedText(pdu.DataCoding, data as string) : data as byte[];
			var maxSegmentLen = GetMaxSegmentLength(pdu.DataCoding, bytes.Length);

			// Remove/Reset data from PDU..
			pdu.ShortMessage = pdu.MessagePayload = null;
			// Remove sequenceNumber.
			pdu.SequenceNumber = 0;
			// Remove UDH header (if set).
			pdu.EsmClass &= ((byte)~NetworkFeatures.UDHI);
			// Remove SMPP segmentation properties..
			pdu.MoreMessagesToSend = null;
			pdu.NumberOfMessages = null;
			pdu.SarTotalSegments = null;
			pdu.SarMsgRefNumber = null;

			// Sending as payload means avoiding all the data splitting logic.. (which is great ;))
			if (method == SmppSarMethod.SendAsPayload)
			{
				pdu.MessagePayload = data;
				return new[] { pdu };
			}

			// Else.. let's do segmentation and the other crappy stuff..
			var udhref = method == SmppSarMethod.UserDataHeader ? new Nullable<byte>(correlationId) : null;
			var segments = SplitMessage(bytes, maxSegmentLen, udhref);
			var totalSegments = segments.Count();
			var segno = 0;

			// If just one segment, send it w/o SAR parameters..
			if (totalSegments < 2)
			{
				pdu.ShortMessage = data;
				return new[] { pdu };
			}

			// Ok, se we need segmentation, let's go ahead an use input PDU as template.
			var template = pdu.GetEncodedPdu();
			// Well save results here..
			var results = new List<SmppSubmitSm>();

			foreach (var segment in segments)
			{
				var packet = new SmppSubmitSm(template);
				
				segno++; // Increase sequence number.
				packet.SequenceNumber = 0; // Remove sequenceNumber.
				packet.ShortMessage = segment; // Set current segment bytes as short message..

				switch (method)
				{
					case SmppSarMethod.UserDataHeader:
						packet.EsmClass |= (byte)NetworkFeatures.UDHI; // Set UDH flag..
						break;
					case SmppSarMethod.UseSmppSegmentation:
						packet.EsmClass &= ((byte)~NetworkFeatures.UDHI); // Remove UDH header (if set).
						// Fill-in SMPP segmentation fields..
						packet.MoreMessagesToSend = segno != totalSegments;
						packet.NumberOfMessages = (byte)totalSegments;
						packet.SarTotalSegments = (byte)totalSegments;
						packet.SarMsgRefNumber = correlationId;
						packet.SarSegmentSeqnum = (byte)segno;
						break;
				}

				result.Add(packet);
			}

			return result;
		}
	}
}
