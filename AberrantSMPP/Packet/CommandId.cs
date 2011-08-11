using System;
using System.Collections.Generic;
using System.Text;

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
		generic_nack = 0x80000000,
		/// <summary>
		/// bind_receiver
		/// </summary>
		bind_receiver = 0x00000001,
		/// <summary>
		/// bind_receiver_resp
		/// </summary>
		bind_receiver_resp = 0x80000001,
		/// <summary>
		/// bind_transmitter
		/// </summary>
		bind_transmitter = 0x00000002,
		/// <summary>
		/// bind_transmitter_resp
		/// </summary>
		bind_transmitter_resp = 0x80000002,
		/// <summary>
		/// query_sm
		/// </summary>
		query_sm = 0x00000003,
		/// <summary>
		/// query_sm_resp
		/// </summary>
		query_sm_resp = 0x80000003,
		/// <summary>
		/// submit_sm
		/// </summary>
		submit_sm = 0x00000004,
		/// <summary>
		/// submit_sm_resp
		/// </summary>
		submit_sm_resp = 0x80000004,
		/// <summary>
		/// deliver_sm
		/// </summary>
		deliver_sm = 0x00000005,
		/// <summary>
		/// deliver_sm_resp
		/// </summary>
		deliver_sm_resp = 0x80000005,
		/// <summary>
		/// unbind
		/// </summary>
		unbind = 0x00000006,
		/// <summary>
		/// unbind_resp
		/// </summary>
		unbind_resp = 0x80000006,
		/// <summary>
		/// replace_sm
		/// </summary>
		replace_sm = 0x00000007,
		/// <summary>
		/// replace_sm_resp
		/// </summary>
		replace_sm_resp = 0x80000007,
		/// <summary>
		/// cancel_sm
		/// </summary>
		cancel_sm = 0x00000008,
		/// <summary>
		/// cancel_sm_resp
		/// </summary>
		cancel_sm_resp = 0x80000008,
		/// <summary>
		/// bind_transceiver
		/// </summary>
		bind_transceiver = 0x00000009,
		/// <summary>
		/// bind_transceiver_resp
		/// </summary>
		bind_transceiver_resp = 0x80000009,
		/// <summary>
		/// outbind
		/// </summary>
		outbind = 0x0000000B,
		/// <summary>
		/// enquire_link
		/// </summary>
		enquire_link = 0x00000015,
		/// <summary>
		/// enquire_link_resp
		/// </summary>
		enquire_link_resp = 0x80000015,
		/// <summary>
		/// submit_multi
		/// </summary>
		submit_multi = 0x00000021,
		/// <summary>
		/// submit_multi_resp
		/// </summary>
		submit_multi_resp = 0x80000021,
		/// <summary>
		/// alert_notification
		/// </summary>
		alert_notification = 0x00000102,
		/// <summary>
		/// data_sm
		/// </summary>
		data_sm = 0x00000103,
		/// <summary>
		/// data_sm_resp
		/// </summary>
		data_sm_resp = 0x80000103
	}
}
