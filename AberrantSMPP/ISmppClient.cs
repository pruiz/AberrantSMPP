using System;
using System.Security.Authentication;

using AberrantSMPP.EventObjects;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;

namespace AberrantSMPP
{
	public interface ISmppClient : IDisposable
	{
		// FIXME: This should expose all that is common to both Smpp client implementations..

		/// <summary>
		/// The host to bind this ISmppClient to.
		/// </summary>
		string Host { get; }

		/// <summary>
		/// The port on the SMSC to connect to.
		/// </summary>
		UInt16 Port { get; }

		/// <summary>
		/// The binding type(receiver, transmitter, or transceiver)to use 
		/// when connecting to the SMSC.
		/// </summary>
		SmppBind.BindingType BindType { get; set; }

		/// <summary>
		/// The system type to use when connecting to the SMSC.
		/// </summary>
		string SystemType { get; set; }

		/// <summary>
		/// The system ID to use when connecting to the SMSC. This is, 
		/// in essence, a user name.
		/// </summary>
		string SystemId { get; set; }

		/// <summary>
		/// The password to use when connecting to an SMSC.
		/// </summary>
		string Password { get; set; }

		/// <summary>
		/// The number plan indicator that this ISmppClient should use.  
		/// </summary>
		Pdu.NpiType NpiType { get; set; }

		/// <summary>
		/// The type of number that this ISmppClient should use.
		/// </summary>
		Pdu.TonType TonType { get; set; }

		/// <summary>
		/// The SMPP specification version to use.
		/// </summary>
		SmppBind.SmppVersionType Version { get; set; }

		/// <summary>
		/// The address range of this SMPPCommunicator.
		/// </summary>
		string AddressRange { get; set; }

		/// <summary>
		/// Set to the interval that should elapse in between enquire_link packets.
		/// Setting this to anything other than 0 will enable the timer, setting 
		/// it to 0 will disable the timer. Timer is started after next connection.
		/// Also, EnquireLink packets are only sent when no other traffic went on between
		/// state interval.
		/// </summary>
		TimeSpan EnquireLinkInterval { get; set; }

		/// <summary>
		/// Get supported SSL Protocols. Must be set on constructor.
		/// </summary>
		SslProtocols SupportedSslProtocols { get; }

		#region events
		/// <summary>
		/// Event called when the client receives a bind response.
		/// </summary>
		event BindRespEventHandler OnBindResp;
		/// <summary>
		/// Event called when the client is unbound.
		/// </summary>
		event UnbindRespEventHandler OnUnboundResp;
		/// <summary>
		/// Event called when the connection is closed.
		/// </summary>
		event ClosingEventHandler OnClose;
		/// <summary>
		/// Event called when an alert_notification comes in.
		/// </summary>
		event AlertEventHandler OnAlert;
		/// <summary>
		/// Event called when a submit_sm_resp is received.
		/// </summary>
		event SubmitSmRespEventHandler OnSubmitSmResp;
		/// <summary>
		/// Event called when a response to an enquire_link_resp is received.
		/// </summary>
		event EnquireLinkRespEventHandler OnEnquireLinkResp;
		/// <summary>
		/// Event called when a submit_sm is received.
		/// </summary>
		event SubmitSmEventHandler OnSubmitSm;
		/// <summary>
		/// Event called when a query_sm is received.
		/// </summary>
		event QuerySmEventHandler OnQuerySm;
		/// <summary>
		/// Event called when a generic_nack is received.
		/// </summary>
		event GenericNackEventHandler OnGenericNack;
		/// <summary>
		/// Event called when a generic_nack is received.
		/// </summary>
		//event GenericNackRespEventHandler OnGenericNackResp;
		/// <summary>
		/// Event called when an enquire_link is received.
		/// </summary>
		event EnquireLinkEventHandler OnEnquireLink;
		/// <summary>
		/// Event called when an unbind is received.
		/// </summary>
		event UnbindEventHandler OnUnbind;
		/// <summary>
		/// Event called when the client receives a request for a bind.
		/// </summary>
		event BindEventHandler OnBind;
		/// <summary>
		/// Event called when the client receives a cancel_sm.
		/// </summary>
		event CancelSmEventHandler OnCancelSm;
		/// <summary>
		/// Event called when the client receives a cancel_sm_resp.
		/// </summary>
		event CancelSmRespEventHandler OnCancelSmResp;
		/// <summary>
		/// Event called when the client receives a query_sm_resp.
		/// </summary>
		event QuerySmRespEventHandler OnQuerySmResp;
		/// <summary>
		/// Event called when the client receives a data_sm.
		/// </summary>
		event DataSmEventHandler OnDataSm;
		/// <summary>
		/// Event called when the client receives a data_sm_resp.
		/// </summary>
		event DataSmRespEventHandler OnDataSmResp;
		/// <summary>
		/// Event called when the client receives a deliver_sm.
		/// </summary>
		event DeliverSmEventHandler OnDeliverSm;
		/// <summary>
		/// Event called when the client receives a deliver_sm_resp.
		/// </summary>
		event DeliverSmRespEventHandler OnDeliverSmResp;
		/// <summary>
		/// Event called when the client receives a replace_sm.
		/// </summary>
		event ReplaceSmEventHandler OnReplaceSm;
		/// <summary>
		/// Event called when the client receives a replace_sm_resp.
		/// </summary>
		event ReplaceSmRespEventHandler OnReplaceSmResp;
		/// <summary>
		/// Event called when the client receives a submit_multi.
		/// </summary>
		event SubmitMultiEventHandler OnSubmitMulti;
		/// <summary>
		/// Event called when the client receives a submit_multi_resp.
		/// </summary>
		event SubmitMultiRespEventHandler OnSubmitMultiResp;
		/// <summary>
		/// Event called when the client' state changes.
		/// </summary>
		//event ClientStateChangedEventHandler OnClientStateChanged;
		/// <summary>
		/// Event called when an error occurs.
		/// </summary>
		event ErrorEventHandler OnError;

		#endregion events

		#region delegates

		/// <summary>
		/// Delegate to handle binding responses of the communicator.
		/// </summary>
		public delegate void BindRespEventHandler(object source, BindRespEventArgs e);
		/// <summary>
		/// Delegate to handle any errors that come up.
		/// </summary>
		public delegate void ErrorEventHandler(object source, SmppExceptionEventArgs e);
		/// <summary>
		/// Delegate to handle the unbind_resp.
		/// </summary>
		public delegate void UnbindRespEventHandler(object source, UnbindRespEventArgs e);
		/// <summary>
		/// Delegate to handle closing of the connection.
		/// </summary>
		public delegate void ClosingEventHandler(object source, EventArgs e);
		/// <summary>
		/// Delegate to handle alert_notification events.
		/// </summary>
		public delegate void AlertEventHandler(object source, AlertEventArgs e);
		/// <summary>
		/// Delegate to handle a submit_sm_resp
		/// </summary>
		public delegate void SubmitSmRespEventHandler(object source, SubmitSmRespEventArgs e);
		/// <summary>
		/// Delegate to handle the enquire_link response.
		/// </summary>
		public delegate void EnquireLinkRespEventHandler(object source, EnquireLinkRespEventArgs e);
		/// <summary>
		/// Delegate to handle the submit_sm.
		/// </summary>
		public delegate void SubmitSmEventHandler(object source, SubmitSmEventArgs e);
		/// <summary>
		/// Delegate to handle the query_sm.
		/// </summary>
		public delegate void QuerySmEventHandler(object source, QuerySmEventArgs e);
		/// <summary>
		/// Delegate to handle generic_nack.
		/// </summary>
		public delegate void GenericNackEventHandler(object source, GenericNackEventArgs e);
		/// <summary>
		/// Delegate to handle generic_nack.
		/// </summary>
		public delegate void GenericNackRespEventHandler(object source, GenericNackRespEventArgs e);
		/// <summary>
		/// Delegate to handle the enquire_link.
		/// </summary>
		public delegate void EnquireLinkEventHandler(object source, EnquireLinkEventArgs e);
		/// <summary>
		/// Delegate to handle the unbind message.
		/// </summary>
		public delegate void UnbindEventHandler(object source, UnbindEventArgs e);
		/// <summary>
		/// Delegate to handle requests for binding of the communicator.
		/// </summary>
		public delegate void BindEventHandler(object source, BindEventArgs e);
		/// <summary>
		/// Delegate to handle cancel_sm.
		/// </summary>
		public delegate void CancelSmEventHandler(object source, CancelSmEventArgs e);
		/// <summary>
		/// Delegate to handle cancel_sm_resp.
		/// </summary>
		public delegate void CancelSmRespEventHandler(object source, CancelSmRespEventArgs e);
		/// <summary>
		/// Delegate to handle query_sm_resp.
		/// </summary>
		public delegate void QuerySmRespEventHandler(object source, QuerySmRespEventArgs e);
		/// <summary>
		/// Delegate to handle data_sm.
		/// </summary>
		public delegate void DataSmEventHandler(object source, DataSmEventArgs e);
		/// <summary>
		/// Delegate to handle data_sm_resp.
		/// </summary>
		public delegate void DataSmRespEventHandler(object source, DataSmRespEventArgs e);
		/// <summary>
		/// Delegate to handle deliver_sm.
		/// </summary>
		public delegate void DeliverSmEventHandler(object source, DeliverSmEventArgs e);
		/// <summary>
		/// Delegate to handle deliver_sm_resp.
		/// </summary>
		public delegate void DeliverSmRespEventHandler(object source, DeliverSmRespEventArgs e);
		/// <summary>
		/// Delegate to handle replace_sm.
		/// </summary>
		public delegate void ReplaceSmEventHandler(object source, ReplaceSmEventArgs e);
		/// <summary>
		/// Delegate to handle replace_sm_resp.
		/// </summary>
		public delegate void ReplaceSmRespEventHandler(object source, ReplaceSmRespEventArgs e);
		/// <summary>
		/// Delegate to handle submit_multi.
		/// </summary>
		public delegate void SubmitMultiEventHandler(object source, SubmitMultiEventArgs e);
		/// <summary>
		/// Delegate to handle submit_multi_resp.
		/// </summary>
		public delegate void SubmitMultiRespEventHandler(object source, SubmitMultiRespEventArgs e);
		/// <summary>
		/// Delegate to handle state changes.
		/// </summary>
		public delegate void ClientStateChangedEventHandler(object source, ClientStateChangedEventArgs e);

		#endregion delegates
	}
}
