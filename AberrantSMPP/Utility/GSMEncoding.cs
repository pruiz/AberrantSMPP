using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AberrantSMPP.Utility
{
	// See: http://www.dreamfabric.com/sms/default_alphabet.html

	// TODO: Create a real Encoding by deriving from System.Text.Encoding.
	// OPTIMIZE: This code is crappy, we shouldn't be using strings internally to do encoding.. (pruiz)
	public class GSM8BitEncoding
	{
		public static byte[] GetBytes(string text)
		{
			using (var ms = new MemoryStream())
			{

				// ` is not a conversion, just a untranslatable letter
				string strGSMTable = "";
				strGSMTable += "@£$¥èéùìòÇ`Øø`Åå";
				strGSMTable += "Δ_ΦΓΛΩΠΨΣΘΞ`ÆæßÉ";
				strGSMTable += " !\"#¤%&'()*=,-./";
				strGSMTable += "0123456789:;<=>?";
				strGSMTable += "¡ABCDEFGHIJKLMNO";
				strGSMTable += "PQRSTUVWXYZÄÖÑÜ`";
				strGSMTable += "¿abcdefghijklmno";
				strGSMTable += "pqrstuvwxyzäöñüà";

				string strExtendedTable = "";
				strExtendedTable += "````````````````";
				strExtendedTable += "````^```````````";
				strExtendedTable += "````````{}`````\\";
				strExtendedTable += "````````````[~]`";
				strExtendedTable += "|```````````````";
				strExtendedTable += "````````````````";
				strExtendedTable += "`````€``````````";
				strExtendedTable += "````````````````";

				foreach (char cPlainText in text.ToCharArray())
				{
					int intGSMTable = strGSMTable.IndexOf(cPlainText);
					if (intGSMTable != -1)
					{
						ms.WriteByte(Convert.ToByte(intGSMTable));
						continue;
					}
					int intExtendedTable = strExtendedTable.IndexOf(cPlainText);
					if (intExtendedTable != -1 && strExtendedTable[intExtendedTable] != '`')
					{
						ms.WriteByte(0x1b); // Escape char..
						ms.WriteByte(Convert.ToByte(intExtendedTable));
					}
				}

				return ms.ToArray();
			}
		}
	}

	public class GSM7BitEnconding
	{
		/// <summary>
		/// Compacts a string of septets into octets.
		/// </summary>
		/// <remarks>
		/// <par>When only 7 of 8 available bits of a character are used, 1 bit is
		/// wasted per character. This method compacts a string of characters
		/// which consist solely of such 7-bit characters.</par>
		/// <par>Effectively, every 8 bytes of the original string are packed into
		/// 7 bytes in the resulting string.</par>
		/// </remarks>
		private static byte[] ConvertTo7Bit(string data)
		{
			var output = new ArrayList();
			string octetSecond = string.Empty;
			for (int i = 0; i < data.Length; i++)
			{
				string current = Convert.ToString((byte)data[i], 2).PadLeft(7, '0');
				if (i != 0 && i % 8 != 0)
				{
					string octetFirst = current.Substring(7 - i % 8);
					string currentOctet = octetFirst + octetSecond;
					output.Add(Convert.ToByte(currentOctet, 2));
				}
				octetSecond = current.Substring(0, 7 - i % 8);
				if (i == data.Length - 1 && octetSecond != string.Empty)
					output.Add(Convert.ToByte(octetSecond, 2));
			}

			byte[] array = new byte[output.Count];
			output.CopyTo(array);
			return array;
		}

		public static byte[] GetBytes(string text)
		{
			throw new NotImplementedException();
		}
	}
}
