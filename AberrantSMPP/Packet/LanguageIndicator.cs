using System;
using System.Collections.Generic;
using System.Text;

namespace AberrantSMPP.Packet
{
	/// <summary>
	/// Enumerates the language types.
	/// </summary>
	public enum LanguageIndicator : byte
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
}
