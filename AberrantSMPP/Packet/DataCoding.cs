using System;
using System.Collections.Generic;
using System.Text;

namespace AberrantSMPP.Packet
{
	/// <summary>
	/// Enumerates the data coding types.
	/// </summary>
	public enum DataCoding : byte
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
}
