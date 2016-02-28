using System;
using System.Collections;
using System.Text;

namespace AberrantSMPP.Utility
{
	// See: http://www.dreamfabric.com/sms/default_alphabet.html

	// TODO: Create a real Encoding by deriving from System.Text.Encoding.
	// OPTIMIZE: Implement unsafe methods to improve performance. (pruiz)
	/// <summary>
	/// GSM (03.38) Encoding class.
	/// </summary>
	public class GsmEncoding : Encoding
	{
		#region Ucs2Gsm Tables.
		private const byte Nochar = 0xFF;
		private const byte Escape = 0x1B;

		private static byte[] _ucs2ToGsm =
		{           
			/*			+0xX0	+0xX1	+0xX2	+0xX3	+0xX4	+0xX5	+0xX6	+0xX7	+0xX8	+0xX9	+0xXa	+0xXb	+0xXc	+0xXd	+0xXe	+0xXf */
			/*0x00*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	0x0a,	Nochar,	Nochar,	0x0D,	Nochar,	Nochar,	
			/*0x10*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0x20*/		0x20,	0x21,	0x22,	0x23,	0x02,	0x25,	0x26,	0x27,	0x28,	0x29,	0x2a,	0x2b,	0x2c,	0x2D,	0x2e,	0x2f,	
			/*0x30*/		0x30,	0x31,	0x32,	0x33,	0x34,	0x35,	0x36,	0x37,	0x38,	0x39,	0x3a,	0x3b,	0x3c,	0x3D,	0x3e,	0x3f,	
			/*0x40*/		0x00,	0x41,	0x42,	0x43,	0x44,	0x45,	0x46,	0x47,	0x48,	0x49,	0x4a,	0x4b,	0x4c,	0x4D,	0x4e,	0x4f,	
			/*0x50*/		0x50,	0x51,	0x52,	0x53,	0x54,	0x55,	0x56,	0x57,	0x58,	0x59,	0x5a,	Escape,	Escape,	Escape,	Escape,	0x11,	
			/*0x60*/		0x27,	0x61,	0x62,	0x63,	0x64,	0x65,	0x66,	0x67,	0x68,	0x69,	0x6a,	0x6b,	0x6c,	0x6D,	0x6e,	0x6f,	
			/*0x70*/		0x70,	0x71,	0x72,	0x73,	0x74,	0x75,	0x76,	0x77,	0x78,	0x79,	0x7a,	Escape,	Escape,	Escape,	Escape,	Nochar,
			/*0x80*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0x90*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0xa0*/		Nochar,	0x40,	Nochar,	0x01,	0x24,	0x03,	Nochar,	0x5f,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0xb0*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	0x60,	
			/*0xc0*/		Nochar,	Nochar,	Nochar,	Nochar,	0x5b,	0x0e,	0x1c,	0x09,	Nochar,	0x1f,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	0x60,	
			/*0xd0*/		Nochar,	0x5D,	Nochar,	Nochar,	Nochar,	Nochar,	0x5c,	Nochar,	0x0b,	Nochar,	Nochar,	Nochar,	0x5e,	Nochar,	Nochar,	0x1e,	
			/*0xe0*/		0x7f,	Nochar,	Nochar,	Nochar,	0x7b,	0x0f,	0x1D,	Nochar,	0x04,	0x05,	Nochar,	Nochar,	0x07,	Nochar,	Nochar,	Nochar,	
			/*0xf0*/		Nochar,	0x7D,	0x08,	Nochar,	Nochar,	Nochar,	0x7c,	Nochar,	0x0c,	0x06,	Nochar,	Nochar,	0x7e,	Nochar,	Nochar,	Nochar
		};

		private static byte[] _ucs2ToGsmExtended =
		{
			/*			+0xX0	+0xX1	+0xX2	+0xX3	+0xX4	+0xX5	+0xX6	+0xX7	+0xX8	+0xX9	+0xXa	+0xXb	+0xXc	+0xXd	+0xXe	+0xXf */
			/*0x0x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	 0x0A,	Nochar,	Nochar,	Nochar,	
			/*0x1x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0x2x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0x3x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0x4x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0x5x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	 0x3c,	 0x2f,	 0x3e,	 0x14,	Nochar,	
			/*0x6x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0x7x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	 0x28,	 0x40,	 0x29,	 0x3d,	Nochar,	
			/*0x8x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0x9x*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0xAx*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0xBx*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0xCx*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0xDx*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0xEx*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	
			/*0xFx*/		Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar,	Nochar
		};

		private const int Ucs2GclToGsmBase = 0x0391;
		private static byte[] _ucs2GclToGsm =
		{
			/*0x0391*/  0x41, // Alpha A
			/*0x0392*/  0x42, // Beta B
			/*0x0393*/  0x13, // Gamma
			/*0x0394*/  0x10, // Delta
			/*0x0395*/  0x45, // Epsilon E
			/*0x0396*/  0x5A, // Zeta Z
			/*0x0397*/  0x48, // Eta H
			/*0x0398*/  0x19, // Theta
			/*0x0399*/  0x49, // Iota I
			/*0x039a*/  0x4B, // Kappa K
			/*0x039b*/  0x14, // Lambda
			/*0x039c*/  0x4D, // Mu M
			/*0x039d*/  0x4E, // Nu N
			/*0x039e*/  0x1A, // Xi
			/*0x039f*/  0x4F, // Omicron O
			/*0x03a0*/  0x16, // Pi
			/*0x03a1*/  0x50, // Rho P
			/*0x03a2*/  Nochar,
			/*0x03a3*/  0x18, // Sigma
			/*0x03a4*/  0x54, // Tau T
			/*0x03a5*/  0x59, // Upsilon Y
			/*0x03a6*/  0x12, // Phi 
			/*0x03a7*/  0x58, // Chi X
			/*0x03a8*/  0x17, // Psi
			/*0x03a9*/  0x15  // Omega
		};
		private static int _ucs2GclToGsmMax = Ucs2GclToGsmBase + _ucs2GclToGsm.Length;
		#endregion

		#region Gsm2Ucs Tables
		private static char _nocode = '\xFFFF';
		private static char _uescape = '\xA0';

		private static char[] _gsmToUcs2 = new char[] {
			/*			+0xX0	+0xX1	+0xX2	+0xX3	+0xX4	+0xX5	+0xX6	+0xX7	+0xX8	+0xX9	+0xXa	+0xXb	+0xXc	+0xXd	+0xXe	+0xXf */
			/*0x0x*/		'\x40',	'\xA3',	'\x24',	'\xA5',	'\xE8',	'\xE9',	'\xF9',	'\xEC',	'\xF2',	'\xE7',	'\x0A',	'\xD8',	'\xF8',	'\x0D',	'\xC5',	'\xE5',	 
			/*0x1x*/		'\x394',	'\x5F',	'\x3A6',	'\x393',	'\x39B',	'\x3A9',	'\x3A0',	'\x3A8',	'\x3A3',	'\x398',	'\x39E',	'\xA0',	'\xC6',	'\xE6',	'\xDF',	'\xC9',	
			/*0x2x*/		'\x20',	'\x21',	'\x22',	'\x23',	'\xA4',	'\x25',	'\x26',	'\x27',	'\x28',	'\x29',	'\x2A',	'\x2B',	'\x2C',	'\x2D',	'\x2E',	'\x2F',	
			/*0x3x*/		'\x30',	'\x31',	'\x32',	'\x33',	'\x34',	'\x35',	'\x36',	'\x37',	'\x38',	'\x39',	'\x3A',	'\x3B',	'\x3C',	'\x3D',	'\x3E',	'\x3F',	
			/*0x4x*/		'\xA1',	'\x41',	'\x42',	'\x43',	'\x44',	'\x45',	'\x46',	'\x47',	'\x48',	'\x49',	'\x4A',	'\x4B',	'\x4C',	'\x4D',	'\x4E',	'\x4F',	
			/*0x5x*/		'\x50',	'\x51',	'\x52',	'\x53',	'\x54',	'\x55',	'\x56',	'\x57',	'\x58',	'\x59',	'\x5A',	'\xC4',	'\xD6',	'\xD1',	'\xDC',	'\xA7',	
			/*0x6x*/		'\xBF',	'\x61',	'\x62',	'\x63',	'\x64',	'\x65',	'\x66',	'\x67',	'\x68',	'\x69',	'\x6A',	'\x6B',	'\x6C',	'\x6D',	'\x6E',	'\x6F',	
			/*0x7x*/		'\x70',	'\x71',	'\x72',	'\x73',	'\x74',	'\x75',	'\x76',	'\x77',	'\x78',	'\x79',	'\x7A',	'\xE4',	'\xF6',	'\xF1',	'\xFC',	'\xE0'
		};

		private static char[] _gsmToUcs2Extended = new char[] {
			/*			+0xX0	+0xX1	+0xX2	+0xX3	+0xX4	+0xX5	+0xX6	+0xX7	+0xX8	+0xX9	+0xXa	+0xXb	+0xXc	+0xXd	+0xXe	+0xXf */
			/*0x0x*/		_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	'\x0C',	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	 
			/*0x1x*/		_nocode,	_nocode,	_nocode,	_nocode,	'\x5E',	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	
			/*0x2x*/		_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	'\x7B',	'\x7D',	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	'\x5C',	
			/*0x3x*/		_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	'\x5B',	'\x7E',	'\x5D',	_nocode,	
			/*0x4x*/		'\x7C',	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	
			/*0x5x*/		_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	
			/*0x6x*/		_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	
			/*0x7x*/		_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode,	_nocode
		};
		#endregion

		#region Instance fields..
		private bool _useBestFitFallback = false;
		private bool _throwOnInvalidCharacter = false;
		private EncoderFallbackBuffer _encoderFb = null;
		private DecoderFallbackBuffer _decoderFb = null;
		#endregion

		#region Properties
		public override string EncodingName
		{
			get
			{
				return "GSMEncoding";
			}
		}
		#endregion

		#region .ctors
		public GsmEncoding() : this (false)
		{
		}

		public GsmEncoding(bool throwOnInvalidCharacter)
		{
			this._throwOnInvalidCharacter = throwOnInvalidCharacter;
		}

		public GsmEncoding(bool useBestFitFallback, bool throwOnInvalidCharacter)
			: this(throwOnInvalidCharacter)
		{
			this._useBestFitFallback = useBestFitFallback;
		}
		#endregion

		#region UCS -> GSM conversion methods..
		public new EncoderFallback EncoderFallback
		{
			get { 
				var obj = _throwOnInvalidCharacter ? EncoderFallback.ExceptionFallback : base.EncoderFallback;
				return _useBestFitFallback ? new GsmBestFitEncoderFallback(obj) : obj;
			}
		}

		private EncoderFallbackBuffer EncoderFallbackBuffer
		{
			get
			{
				if (_encoderFb == null)
					_encoderFb = EncoderFallback.CreateFallbackBuffer();

				return _encoderFb;
			}
		}

		public static bool IsValidChar(char character)
		{
			var @byte = Nochar;
			var escape = false;

			if (character < _ucs2ToGsm.Length)
			{
				@byte = _ucs2ToGsm[character];

				if (@byte == Escape)
				{
					escape = true;
					@byte = _ucs2ToGsmExtended[character];
				}
			}
			else if (character >= Ucs2GclToGsmBase && character <= _ucs2GclToGsmMax)
			{
				escape = true;
				@byte = _ucs2GclToGsm[character - Ucs2GclToGsmBase];
			}
			else if (character == '\x20AC') // Euro sign.
			{
				escape = true;
				@byte = 0x65;
			}

			return @byte != Nochar;
		}

		public static bool IsValidString(string text)
		{
			foreach (var character in text.ToCharArray())
			{
				var @byte = Nochar;
				var escape = false;

				if (character < _ucs2ToGsm.Length)
				{
					@byte = _ucs2ToGsm[character];

					if (@byte == Escape)
					{
						escape = true;
						@byte = _ucs2ToGsmExtended[character];
					}
				}
				else if (character >= Ucs2GclToGsmBase && character <= _ucs2GclToGsmMax)
				{
					escape = true;
					@byte = _ucs2GclToGsm[character - Ucs2GclToGsmBase];
				}
				else if (character == '\x20AC') // Euro sign.
				{
					escape = true;
					@byte = 0x65;
				}

				if (@byte == Nochar)
					return false;
			}

			return true;
		}

		private int GetBytesInternal(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
		{
			var outpos = byteIndex;

			for (var inpos = charIndex; inpos < (charIndex + charCount); inpos++)
			{
				var character = chars[inpos];
				var @byte = Nochar;
				var escape = false;

				if (character < _ucs2ToGsm.Length)
				{
					@byte = _ucs2ToGsm[character];

					if (@byte == Escape)
					{
						escape = true;
						@byte = _ucs2ToGsmExtended[character];
					}
				}
				else if (character >= Ucs2GclToGsmBase && character <= _ucs2GclToGsmMax)
				{
					escape = true;
					@byte = _ucs2GclToGsm[character - Ucs2GclToGsmBase];
				}
				else if (character == '\x20AC') // Euro sign.
				{
					escape = true;
					@byte = 0x65;
				}

				if (@byte == Nochar)
				{
					char tmp;
					EncoderFallbackBuffer.Fallback(character, inpos);

					while ((tmp = EncoderFallbackBuffer.GetNextChar()) != 0)
					{
						if (bytes != null)
							bytes[outpos++] = _ucs2ToGsm[tmp]; // FIXME: Character might not be a 7-bit one..
						else
							outpos++;
					}

					EncoderFallbackBuffer.Reset();
				}
				else
				{
					if (bytes != null)
					{
						if (escape)
							bytes[outpos++] = Escape;
						bytes[outpos++] = @byte;
					}
					else
					{
						outpos += escape ? 2 : 1;
					}
				}
			}

			return outpos - byteIndex;
		}

		public override int GetMaxByteCount(int charCount)
		{
			return charCount * 2;
		}

#if UNSAFE_CODE
		public unsafe override int GetByteCount(char* chars, int count)
		{
			var ret = 0;
			var end = chars + count;

			for (; chars < end; chars++) {
				var character = *chars;

				if (character < Ucs2ToGsm.Length)
				{
					var tmp = Ucs2ToGsm[character];

					if (tmp == NOCHAR) ret += gsm_fb_len; // Fallback char
					if (tmp == ESCAPE) ret += 2;
					else ret += 1;
				}
				else if (character >= Ucs2GclToGsmBase && character <= Ucs2GclToGsmMax)
				{
					var tmp = Ucs2GclToGsm[character - Ucs2GclToGsmBase];

					if (tmp == NOCHAR) ret += gsm_fb_len; // Fallback char
					else ret += 2;
				}
				else if (character == '\x20AC') // Euro sign.
				{
					ret += 2;
				}
				else
				{
					ret += gsm_fb_len; // FallBack char.
				}
			}

			return ret;
		}
#endif

		public override int GetByteCount(char[] chars, int index, int count)
		{
			if (chars == null) {
				throw new ArgumentNullException ("chars");
			}
			if (index < 0 || index > chars.Length) {
				throw new ArgumentOutOfRangeException ("index");
			}
			if (count < 0 || count > (chars.Length - index)) {
				throw new ArgumentOutOfRangeException ("count");
			}

#if UNSAFE_CODE
			unsafe
			{
				fixed (char* chptr = chars)
				{
					return GetByteCount(chptr + index, count);
				}
			}
#else
			return GetBytesInternal(chars, index, count, null, 0);
#endif
		}

		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
		{
			return GetBytesInternal(chars, charIndex, charCount, bytes, byteIndex);
		}
		#endregion

		#region GSM -> UCS conversion methods..
		private DecoderFallbackBuffer DecoderFallbackBuffer
		{
			get
			{
				if (_decoderFb == null)
					_decoderFb = DecoderFallback.CreateFallbackBuffer();

				return _decoderFb;
			}
		}

		public new DecoderFallback DecoderFallback
		{
			get { return _throwOnInvalidCharacter ? DecoderFallback.ExceptionFallback : base.DecoderFallback; }
		}

		private int GetCharsInternal(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			var outpos = byteIndex;
			var escape = false;

			for (var inpos = byteIndex; inpos < (byteIndex + byteCount); inpos++)
			{
				var @byte = bytes[inpos];
				var codepoint = _nocode;
				var extended = false;

				if (escape)
				{
					if (@byte == 0x65) codepoint = '\x20AC';
					else codepoint = _gsmToUcs2Extended[@byte];

					// If char is not a valid codepoint, use NBSP.
					if (codepoint == _nocode) codepoint = '\xA0';

					extended = true;
					escape = false;
				}
				else if (@byte < _gsmToUcs2.Length)
				{
					codepoint = _gsmToUcs2[@byte];

					if (codepoint == _uescape)
					{
						escape = true;
						continue;
					}
				}

				if (codepoint == _nocode)
				{
					char tmp;
					DecoderFallbackBuffer.Fallback(extended ? new byte[] { 0x1b, @byte } : new[] { @byte }, inpos);

					while ((tmp = DecoderFallbackBuffer.GetNextChar()) != 0)
					{
						if (chars != null)
							chars[outpos++] = _gsmToUcs2[tmp]; // FIXME: Character might not be a 7-bit one..
						else
							outpos++;
					}
				}
				else
				{
					if (chars != null)
					{
						chars[outpos++] = codepoint;
					}
					else
					{
						outpos += 1;
					}
				}
			}

			return outpos - charIndex;
		}

		public override int GetMaxCharCount(int byteCount)
		{
			return byteCount;
		}

		public override int GetCharCount(byte[] bytes, int index, int count)
		{
			return GetCharsInternal(bytes, index, count, null, 0);
		}

		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			return GetCharsInternal(bytes, byteIndex, byteCount, chars, charIndex);
		}
		#endregion
	}

	public class PackedGsmEnconding
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

	internal class GsmBestFitEncoderFallback : EncoderFallback
	{
		#region Fallback Buffer implementation
		public class GsmBestFitEncoderFallbackBuffer : EncoderFallbackBuffer
		{
			private EncoderFallbackBuffer _lastResortEncoderFallbackBuffer;
			private char? _data = null;
			private int _pos = 0;
			#region Character Table Mappings..
			// ÁÉÍÓÚ ÀÈÌÒÙ áéíóú 
			private static char[][] _mapTable = new char[][] {
				new char[] { 'Á', 'à' },
				new char[] { 'Í', 'ì' },
				new char[] { 'Ó', 'ò' },
				new char[] { 'Ú', 'ù' },

				new char[] { 'À', 'à' },
				new char[] { 'È', 'É' },
				new char[] { 'Ì', 'ì' },
				new char[] { 'Ò', 'ò' },
				new char[] { 'Ù', 'ù' },

				new char[] { 'á', 'à' },
				new char[] { 'í', 'ì' },
				new char[] { 'ó', 'ò' },
				new char[] { 'ú', 'ù' },

			};
			#endregion

			public override int Remaining { get { return _pos == 0 ? 1 : 0; } }

			public GsmBestFitEncoderFallbackBuffer(EncoderFallback caller, EncoderFallback lastResortFallback)
			{
				_lastResortEncoderFallbackBuffer = lastResortFallback.CreateFallbackBuffer();
			}

			private void InitData(char ch)
			{
				_data = ch;
				_pos = 0;
			}

			public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
			{
				if (_data.HasValue)
					throw new InvalidOperationException("Recursive fallback buffer call.");

				return _lastResortEncoderFallbackBuffer.Fallback(charUnknownHigh, charUnknownLow, index);
			}

			public override bool Fallback(char charUnknown, int index)
			{
				if (_data.HasValue)
					throw new InvalidOperationException("Recursive fallback buffer call.");

				foreach (var entry in _mapTable)
				{
					if (entry[0] == charUnknown)
					{
						InitData(entry[1]);
						return true;
					}
				}

				return _lastResortEncoderFallbackBuffer.Fallback(charUnknown, index);
			}

			public override char GetNextChar()
			{
				if (!_data.HasValue)
					return _lastResortEncoderFallbackBuffer.GetNextChar();

				return _pos++ == 0 ? _data.Value : '\0';
			}

			public override bool MovePrevious()
			{
				if (_data.HasValue)
					return _lastResortEncoderFallbackBuffer.MovePrevious();

				_pos = 0;

				return true;
			}

			public override void Reset()
			{
				_data = null;
				_pos = 0;
				base.Reset();
			}
		}
		#endregion

		private EncoderFallback _lastResortEncoderFallback;

		public override int MaxCharCount { get { return 1; } }

		public GsmBestFitEncoderFallback(EncoderFallback lastResortEncoderFallback)
		{
			this._lastResortEncoderFallback = lastResortEncoderFallback;
		}

		public override EncoderFallbackBuffer CreateFallbackBuffer()
		{
			return new GsmBestFitEncoderFallbackBuffer(this, _lastResortEncoderFallback);
		}
	}
}
