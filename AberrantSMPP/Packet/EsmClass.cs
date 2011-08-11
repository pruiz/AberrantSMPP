using System;
using System.Collections.Generic;
using System.Text;

namespace AberrantSMPP.Packet
{
	/// <summary>
	/// EsmClass' messaging mode flags.
	/// </summary>
	public enum MessagingMode : byte
	{
		// Messaging Mode (bits 1-0)
		/// <summary>
		/// Default SMSC Mode (e.g. Store and Forward)
		/// </summary>
		SMSCDefault	= 0x00,
		/// <summary>
		/// Datagram mode
		/// </summary>
		Datagram = 0x01,
		/// <summary>
		/// Forward (i.e. Transaction) mode
		/// </summary>
		Transactional = 0x02,
		/// <summary>
		/// Store and Forward mode
		/// </summary>
		/// <remarks>
		/// Use to select Store and Forward mode if Default SMSC Mode is non Store and Forward
		/// </remarks>
		StoreAndForward = 0x03
	}

	/// <summary>
	/// EsmClass' messaging type flags. (bits 5-2)
	/// </summary>
	public enum MessageType : byte
	{
		/// <summary>
		/// Default message Type (i.e. normal message)
		/// </summary>
		Default = 0,
		/// <summary>
		/// Short Message contains SMSC Delivery Receipt
		/// </summary>
		SmscDeliveryReceipt = (1<<2),
		/// <summary>
		/// Short Message contains ESME Delivery Acknowledgement
		/// </summary>
		EsmeDeliveryAck = (1<<3),
		/// <summary>
		/// Short Message contains ESME Manual/User Acknowledgement
		/// </summary>
		EsmeUserAck = (1<<4),
		/// <summary>
		/// Short Message contains Conversation Abort (Korean CDMA)
		/// </summary>
		ConversationAbort = (1<<3) | (1<<4),
		/// <summary>
		/// Short Message contains Intermediate Delivery Notification
		/// </summary>
		SmscIntermAck = (1<<5)
	}

	/// <summary>
	/// EsmClass' GSM Network Specific Features (bits 7-6)
	/// </summary>
	public enum NetworkFeatures : byte
	{
		/// <summary>
		/// No specific features selected
		/// </summary>
		None = 0,
		/// <summary>
		/// UDHI Indicator (only relevant for MT short messages)
		/// </summary>
		UDHI = (1 << 6),
		/// <summary>
		/// Set Reply Path (only relevant for GSM network)
		/// </summary>
		SetReplyPath = (1 << 7)
	}
}
