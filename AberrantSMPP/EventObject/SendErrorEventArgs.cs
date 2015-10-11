using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AberrantSMPP.Packet;

namespace AberrantSMPP.EventObjects
{
	public class SendErrorEventArgs : CommonErrorEventArgs
	{
		private byte[] _data;

		public Pdu Packet { get; private set; }
		public byte[] Data { get { return (byte[])_data.Clone(); } }

		public SendErrorEventArgs(Pdu packet, byte[] data, Exception exception)
			: base(exception)
		{
			Packet = packet;
			_data = data;
		}
	}
}
