namespace AberrantSMPP.Packet
{
	/// <summary>
	/// Enumerates all of the "standard" optional codes.  This is more for
	/// convenience when writing/updating this library than for end programmers,
	/// as the the TLV table methods take a ushort/UInt16 rather than an
	/// OptionalParamCodes enumeration.
	/// </summary>
	public enum OptionalParamCodes : ushort
	{
		/// <summary>
		/// Destination address subunit
		/// </summary>
		DestAddrSubunit = 0x0005,
		/// <summary>
		/// Destination address network type
		/// </summary>
		DestNetworkType = 0x0006,
		/// <summary>
		/// Destination address bearer type
		/// </summary>
		DestBearerType = 0x0007,
		/// <summary>
		/// Destination address telematics ID
		/// </summary>
		DestTelematicsId = 0x0008,
		/// <summary>
		/// Source address subunit
		/// </summary>
		SourceAddrSubunit = 0x000D,
		/// <summary>
		/// Source address network type
		/// </summary>
		SourceNetworkType = 0x000E,
		/// <summary>
		/// Source address bearer type
		/// </summary>
		SourceBearerType = 0x000F,
		/// <summary>
		/// Source address telematics ID
		/// </summary>
		SourceTelematicsId = 0x0010,
		/// <summary>
		/// Quality of service time to live
		/// </summary>
		QosTimeToLive = 0x0017,
		/// <summary>
		/// Payload type
		/// </summary>
		PayloadType = 0x0019,
		/// <summary>
		/// Additional status info
		/// </summary>
		AdditionalStatusInfoText = 0x001D,
		/// <summary>
		/// Receipted message ID
		/// </summary>
		ReceiptedMessageId = 0x001E,
		/// <summary>
		/// Message wait facilities
		/// </summary>
		MsMsgWaitFacilities = 0x0030,
		/// <summary>
		/// Privacy indicator
		/// </summary>
		PrivacyIndicator = 0x0201,
		/// <summary>
		/// Source subaddress
		/// </summary>
		SourceSubaddress = 0x0202,
		/// <summary>
		/// Destination subaddress
		/// </summary>
		DestSubaddress = 0x0203,
		/// <summary>
		/// User message reference
		/// </summary>
		UserMessageReference = 0x0204,
		/// <summary>
		/// User response code
		/// </summary>
		UserResponseCode = 0x0205,
		/// <summary>
		/// Source port
		/// </summary>
		SourcePort = 0x020A,
		/// <summary>
		/// Destination port
		/// </summary>
		DestinationPort = 0x020B,
		/// <summary>
		/// Message reference number
		/// </summary>
		SarMsgRefNum = 0x020C,
		/// <summary>
		/// Language indicator
		/// </summary>
		LanguageIndicator = 0x020D,
		/// <summary>
		/// Total segments
		/// </summary>
		SarTotalSegments = 0x020E,
		/// <summary>
		/// Segment sequence number
		/// </summary>
		SarSegmentSeqnum = 0x020F,
		/// <summary>
		/// Interface version
		/// </summary>
		ScInterfaceVersion = 0x0210,
		/// <summary>
		/// Callback number indicator
		/// </summary>
		CallbackNumPresInd = 0x0302,
		/// <summary>
		/// Callback number tag
		/// </summary>
		CallbackNumAtag = 0x0303,
		/// <summary>
		/// Total number of messages
		/// </summary>
		NumberOfMessages = 0x0304,
		/// <summary>
		/// Callback number
		/// </summary>
		CallbackNum = 0x0381,
		/// <summary>
		/// DPF result
		/// </summary>
		DpfResult = 0x0420,
		/// <summary>
		/// Set DPF
		/// </summary>
		SetDpf = 0x0421,
		/// <summary>
		/// Availability status
		/// </summary>
		MsAvailabilityStatus = 0x0422,
		/// <summary>
		/// Network error code
		/// </summary>
		NetworkErrorCode = 0x0423,
		/// <summary>
		/// Message payload
		/// </summary>
		MessagePayload = 0x0424,
		/// <summary>
		/// Reason for delivery failure
		/// </summary>
		DeliveryFailureReason = 0x0425,
		/// <summary>
		/// More messages to send flag
		/// </summary>
		MoreMessagesToSend = 0x0426,
		/// <summary>
		/// Message state
		/// </summary>
		MessageState = 0x0427,
		/// <summary>
		/// USSD service opcode
		/// </summary>
		UssdServiceOp = 0x0501,
		/// <summary>
		/// Display time
		/// </summary>
		DisplayTime = 0x1201,
		/// <summary>
		/// SMS signal
		/// </summary>
		SmsSignal = 0x1203,
		/// <summary>
		/// Message validity
		/// </summary>
		MsValidity = 0x1204,
		/// <summary>
		/// Alert on message delivery
		/// </summary>
		AlertOnMessageDelivery = 0x130C,
		/// <summary>
		/// ITS reply type
		/// </summary>
		ItsReplyType = 0x1380,
		/// <summary>
		/// ITS session info
		/// </summary>
		ItsSessionInfo = 0x1383
	}
}
