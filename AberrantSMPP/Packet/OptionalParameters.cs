using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		dest_addr_subunit = 0x0005,
		/// <summary>
		/// Destination address network type
		/// </summary>
		dest_network_type = 0x0006,
		/// <summary>
		/// Destination address bearer type
		/// </summary>
		dest_bearer_type = 0x0007,
		/// <summary>
		/// Destination address telematics ID
		/// </summary>
		dest_telematics_id = 0x0008,
		/// <summary>
		/// Source address subunit
		/// </summary>
		source_addr_subunit = 0x000D,
		/// <summary>
		/// Source address network type
		/// </summary>
		source_network_type = 0x000E,
		/// <summary>
		/// Source address bearer type
		/// </summary>
		source_bearer_type = 0x000F,
		/// <summary>
		/// Source address telematics ID
		/// </summary>
		source_telematics_id = 0x0010,
		/// <summary>
		/// Quality of service time to live
		/// </summary>
		qos_time_to_live = 0x0017,
		/// <summary>
		/// Payload type
		/// </summary>
		payload_type = 0x0019,
		/// <summary>
		/// Additional status info
		/// </summary>
		additional_status_info_text = 0x001D,
		/// <summary>
		/// Receipted message ID
		/// </summary>
		receipted_message_id = 0x001E,
		/// <summary>
		/// Message wait facilities
		/// </summary>
		ms_msg_wait_facilities = 0x0030,
		/// <summary>
		/// Privacy indicator
		/// </summary>
		privacy_indicator = 0x0201,
		/// <summary>
		/// Source subaddress
		/// </summary>
		source_subaddress = 0x0202,
		/// <summary>
		/// Destination subaddress
		/// </summary>
		dest_subaddress = 0x0203,
		/// <summary>
		/// User message reference
		/// </summary>
		user_message_reference = 0x0204,
		/// <summary>
		/// User response code
		/// </summary>
		user_response_code = 0x0205,
		/// <summary>
		/// Source port
		/// </summary>
		source_port = 0x020A,
		/// <summary>
		/// Destination port
		/// </summary>
		destination_port = 0x020B,
		/// <summary>
		/// Message reference number
		/// </summary>
		sar_msg_ref_num = 0x020C,
		/// <summary>
		/// Language indicator
		/// </summary>
		language_indicator = 0x020D,
		/// <summary>
		/// Total segments
		/// </summary>
		sar_total_segments = 0x020E,
		/// <summary>
		/// Segment sequence number
		/// </summary>
		sar_segment_seqnum = 0x020F,
		/// <summary>
		/// Interface version
		/// </summary>
		SC_interface_version = 0x0210,
		/// <summary>
		/// Callback number indicator
		/// </summary>
		callback_num_pres_ind = 0x0302,
		/// <summary>
		/// Callback number tag
		/// </summary>
		callback_num_atag = 0x0303,
		/// <summary>
		/// Total number of messages
		/// </summary>
		number_of_messages = 0x0304,
		/// <summary>
		/// Callback number
		/// </summary>
		callback_num = 0x0381,
		/// <summary>
		/// DPF result
		/// </summary>
		dpf_result = 0x0420,
		/// <summary>
		/// Set DPF
		/// </summary>
		set_dpf = 0x0421,
		/// <summary>
		/// Availability status
		/// </summary>
		ms_availability_status = 0x0422,
		/// <summary>
		/// Network error code
		/// </summary>
		network_error_code = 0x0423,
		/// <summary>
		/// Message payload
		/// </summary>
		message_payload = 0x0424,
		/// <summary>
		/// Reason for delivery failure
		/// </summary>
		delivery_failure_reason = 0x0425,
		/// <summary>
		/// More messages to send flag
		/// </summary>
		more_messages_to_send = 0x0426,
		/// <summary>
		/// Message state
		/// </summary>
		message_state = 0x0427,
		/// <summary>
		/// USSD service opcode
		/// </summary>
		ussd_service_op = 0x0501,
		/// <summary>
		/// Display time
		/// </summary>
		display_time = 0x1201,
		/// <summary>
		/// SMS signal
		/// </summary>
		sms_signal = 0x1203,
		/// <summary>
		/// Message validity
		/// </summary>
		ms_validity = 0x1204,
		/// <summary>
		/// Alert on message delivery
		/// </summary>
		alert_on_message_delivery = 0x130C,
		/// <summary>
		/// ITS reply type
		/// </summary>
		its_reply_type = 0x1380,
		/// <summary>
		/// ITS session info
		/// </summary>
		its_session_info = 0x1383
	}
}
