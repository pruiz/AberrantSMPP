namespace AberrantSMPP.Packet
{
	/// <summary>
	/// Enumeration of all the Pdu command types.
	/// </summary>
	public enum CommandId : uint
	{
		/// <summary>
		/// generic_nack
		/// </summary>
		GenericNack = 0x80000000,
		/// <summary>
		/// bind_receiver
		/// </summary>
		BindReceiver = 0x00000001,
		/// <summary>
		/// bind_receiver_resp
		/// </summary>
		BindReceiverResp = 0x80000001,
		/// <summary>
		/// bind_transmitter
		/// </summary>
		BindTransmitter = 0x00000002,
		/// <summary>
		/// bind_transmitter_resp
		/// </summary>
		BindTransmitterResp = 0x80000002,
		/// <summary>
		/// query_sm
		/// </summary>
		QuerySm = 0x00000003,
		/// <summary>
		/// query_sm_resp
		/// </summary>
		QuerySmResp = 0x80000003,
		/// <summary>
		/// submit_sm
		/// </summary>
		SubmitSm = 0x00000004,
		/// <summary>
		/// submit_sm_resp
		/// </summary>
		SubmitSmResp = 0x80000004,
		/// <summary>
		/// deliver_sm
		/// </summary>
		DeliverSm = 0x00000005,
		/// <summary>
		/// deliver_sm_resp
		/// </summary>
		DeliverSmResp = 0x80000005,
		/// <summary>
		/// unbind
		/// </summary>
		Unbind = 0x00000006,
		/// <summary>
		/// unbind_resp
		/// </summary>
		UnbindResp = 0x80000006,
		/// <summary>
		/// replace_sm
		/// </summary>
		ReplaceSm = 0x00000007,
		/// <summary>
		/// replace_sm_resp
		/// </summary>
		ReplaceSmResp = 0x80000007,
		/// <summary>
		/// cancel_sm
		/// </summary>
		CancelSm = 0x00000008,
		/// <summary>
		/// cancel_sm_resp
		/// </summary>
		CancelSmResp = 0x80000008,
		/// <summary>
		/// bind_transceiver
		/// </summary>
		BindTransceiver = 0x00000009,
		/// <summary>
		/// bind_transceiver_resp
		/// </summary>
		BindTransceiverResp = 0x80000009,
		/// <summary>
		/// outbind
		/// </summary>
		Outbind = 0x0000000B,
		/// <summary>
		/// enquire_link
		/// </summary>
		EnquireLink = 0x00000015,
		/// <summary>
		/// enquire_link_resp
		/// </summary>
		EnquireLinkResp = 0x80000015,
		/// <summary>
		/// submit_multi
		/// </summary>
		SubmitMulti = 0x00000021,
		/// <summary>
		/// submit_multi_resp
		/// </summary>
		SubmitMultiResp = 0x80000021,
		/// <summary>
		/// alert_notification
		/// </summary>
		AlertNotification = 0x00000102,
		/// <summary>
		/// data_sm
		/// </summary>
		DataSm = 0x00000103,
		/// <summary>
		/// data_sm_resp
		/// </summary>
		DataSmResp = 0x80000103
	}
}
