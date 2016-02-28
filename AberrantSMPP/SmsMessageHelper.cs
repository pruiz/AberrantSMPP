using System;
using System.Linq;
using System.Text;

namespace AberrantSMPP
{
    public enum MessageCoding
    {
        /// <summary>
        /// 7 Bit
        /// </summary>
        GSM8,

        /// <summary>
        /// 8 Bit
        /// </summary>
        Binary,

        /// <summary>
        /// 16 Bit
        /// </summary>
        Unicode
    }

    /// <summary>
    /// Helper class to send long texts. Converted to C# from https://gist.github.com/foreverdeepak/9130661
    /// </summary>
    public class SmsMessageHelper
    {

        private const byte UDHIE_HEADER_LENGTH = 0x05; //Length of UDH ( 5 bytes)
        private const byte UDHIE_IDENTIFIER_SAR = 0x00; //Indicator for concatenated message
        private const byte UDHIE_SAR_LENGTH = 0x03; // Subheader Length ( 3 bytes)

        /// <summary>
        /// Refer: https://en.wikipedia.org/wiki/GSM_03.38
        /// </summary>
        private static readonly char[] Charset7Bit = {
            '@', '£', '$', '¥', 'è', 'é', 'ù', 'ì', 'ò', 'Ç', '\n', 'Ø', 'ø', '\r', 'Å', 'å', 'Δ', '_', 'Φ', 'Γ', 'Λ', 'Ω',
            'Π', 'Ψ', 'Σ', 'Θ', 'Ξ', 'Æ', 'æ', 'ß', 'É', ' ', '!', '"', '#', '¤', '%', '&', '\'', '(', ')', '*', '+',
            ',', '-', '.', '/', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?', '¡', 'A',
            'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W',
            'X', 'Y', 'Z', 'Ä', 'Ö', 'Ñ', 'Ü', '§', '¿', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'ä', 'ö', 'ñ', 'ü', 'à'
        };

        /// <summary>
        /// Refer: https://en.wikipedia.org/wiki/GSM_03.38
        /// </summary>
        private static readonly char[] Charset7BitExt = { '\f', '^', '{', '}', '\\', '[', '~', ']', '|', '€' };

        private static byte[] GetBytes(string str)
        {
            byte[] array = Encoding.UTF8.GetBytes(str);
            return array;
        }

        /// <summary>
        /// Splits a message into byte arrays taking the characters into account. 
        /// ASCII or Unicode are treated differently.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="coding"></param>
        /// <returns></returns>
        public static byte[][] SplitMessage(string message, out MessageCoding coding)
        {
            coding = MessageCoding.GSM8;
            decimal parts = 1;
            var part = 1;
            var chars_used = 0;
            var chars_sms = 160;

            // find the message coding. 
            foreach (var m in message.Trim())
            {
                if (Charset7Bit.Any(c => c == m))
                {
                    chars_used = chars_used + 1;
                }
                else if (Charset7BitExt.Any(c => c == m))
                {
                    chars_used = chars_used + 2;
                }
                else {
                    coding = MessageCoding.Unicode;
                    chars_used = message.Length;
                    break;
                }
            }

            var fixedString = message.Trim();//AppendEscToAllExtended(message.Trim());
            var textBytes = GetBytes(fixedString);
            int maximumMultipartMessageSegmentSize = ((coding == MessageCoding.Unicode) ? 67 : 134);  // number of characters
            byte[] byteSingleMessage = textBytes;
            byte[][] byteMessagesArray = null;//SplitUnicodeMessage(byteSingleMessage, maximumMultipartMessageSegmentSize);

            if (coding == MessageCoding.GSM8)
            {
                if (chars_used > 160)
                {
                    // split
                    byteMessagesArray = SplitUnicodeMessage(byteSingleMessage, maximumMultipartMessageSegmentSize);
                }
                else {
                    // normal
                    byte[][] segments = new byte[1][];
                    segments[0] = byteSingleMessage;
                    byteMessagesArray = segments;
                }
            }
            else {
                if (chars_used > 70)
                {
                    byteMessagesArray = SplitUnicodeMessage(byteSingleMessage, maximumMultipartMessageSegmentSize);
                }
                else {
                    // normal
                    byte[][] segments = new byte[1][];
                    segments[0] = byteSingleMessage;
                    byteMessagesArray = segments;
                }
            }

            return byteMessagesArray;
        }

        private static byte[][] SplitUnicodeMessage(byte[] aMessage, int maximumMultipartMessageSegmentSize)
        {
            // determine how many messages have to be sent
            int numberOfSegments = aMessage.Length / maximumMultipartMessageSegmentSize;
            int messageLength = aMessage.Length;
            if (numberOfSegments > 255)
            {
                numberOfSegments = 255;
                messageLength = numberOfSegments * maximumMultipartMessageSegmentSize;
            }
            if ((messageLength % maximumMultipartMessageSegmentSize) > 0)
            {
                numberOfSegments++;
            }

            // prepare array for all of the msg segments
            byte[][] segments = new byte[numberOfSegments][];

            // generate new reference number
            // message identification - can be any hexadecimal
            // number but needs to match the UDH Reference Number of all concatenated SMS
            byte[] referenceNumber = new byte[1];
            new Random().NextBytes(referenceNumber);

            // split the message adding required headers
            for (int i = 0; i < numberOfSegments; i++)
            {
                int lengthOfData;
                if (numberOfSegments - i == 1)
                {
                    lengthOfData = messageLength - i * maximumMultipartMessageSegmentSize;
                }
                else {
                    lengthOfData = maximumMultipartMessageSegmentSize;
                }
                // new array to store the header
                segments[i] = new byte[6 + lengthOfData];

                // UDH header
                // doesn't include itself, its header length
                segments[i][0] = UDHIE_HEADER_LENGTH;
                // SAR identifier
                segments[i][1] = UDHIE_IDENTIFIER_SAR;
                // SAR length
                segments[i][2] = UDHIE_SAR_LENGTH;
                // reference number (same for all messages)
                segments[i][3] = referenceNumber[0];
                // total number of segments
                segments[i][4] = (byte)numberOfSegments;
                // segment number
                segments[i][5] = (byte)(i + 1);
                // copy the data into the array
                Array.Copy(aMessage, (i * maximumMultipartMessageSegmentSize), segments[i], 6, lengthOfData);
            }
            return segments;
        }
    }
}