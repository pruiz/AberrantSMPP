using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit;
using NUnit.Framework;

using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace AberrantSMPP.Tests
{
	[TestFixture]
	public class PacketTest
	{
		public static byte[] StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}

		#region DeliverSm
		[Test]
		public void CanDecodeDeliverSm_01()
		{
			var hexBytes = "000000dd0000000500000000019182410001013334363439323836383039000501657669636572746961000400000000000000008569643a323533303932393134353232363637333732207375623a30303120646c7672643a303031207375626d697420646174653a3133303932393136353220646f6e6520646174653a3133303932393136353220737461743a44454c49565244206572723a3030303020746578743a1b3c657669534d531b3e0a534d532064652050727565042300030300000427000102001e001332353330393239313435323236363733373200";
			var packet = StringToByteArray(hexBytes);
			var pdu = new SmppDeliverSm(packet);

			Assert.AreEqual("253092914522667372", pdu.ReceiptedMessageId);
		}
		#endregion
	}
}
