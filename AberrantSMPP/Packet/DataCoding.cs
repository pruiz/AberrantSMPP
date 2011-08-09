using System;
using System.Collections.Generic;
using System.Text;

namespace AberrantSMPP.Packet
{
	/// <summary>
	/// Enumerates the data coding types.
	/// </summary>
	/// <remarks>
	/// See SMPP Spec v3.4, 5.2.19 - data_coding
	/// </remarks>
	public enum DataCoding : byte
	{
		/// <summary>
		/// SMSCDefault (GSM 03.38 - 7bit)
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
		/// JIS (X-0208-1990)
		/// </summary>
		JIS = 0x05,
		/// <summary>
		/// Cyrillic (ISO-8859-5)
		/// </summary>
		Cyrillic = 0x06,
		/// <summary>
		/// Latin_Hebrew (ISO-8859-8)
		/// </summary>
		Latin_Hebrew = 0x07,
		/// <summary>
		/// UTF16/UCS2 (ISO/IEC-10646)
		/// </summary>
		UCS2 = 0x08,
		/// <summary>
		/// Pictogram 
		/// </summary>
		Pictogram = 0x09,
		/// <summary>
		/// MusicCodes (ISO-2022-JP)
		/// </summary>
		MusicCodes = 0x0A,
		/// <summary>
		/// ExtendedKanjiJIS (X-0212-1990)
		/// </summary>
		ExtendedKanjiJIS = 0x0D,
		/// <summary>
		/// KS_C (5601)
		/// </summary>
		KS_C = 0x0E
	}
}
