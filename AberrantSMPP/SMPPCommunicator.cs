/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 * Copyright (C) 2010, 2011 Pablo Ruiz García <pruiz@crt0.net>
 *
 * This file is part of RoaminSMPP.
 *
 * RoaminSMPP is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, version 3 of the License.
 *
 * RoaminSMPP is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.ComponentModel;
using System.Security.Authentication;
using System.Runtime.CompilerServices;

using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;
using AberrantSMPP.Exceptions;
using AberrantSMPP.EventObjects;
using AberrantSMPP.Utility;

namespace AberrantSMPP 
{
	/// <summary>
	/// Wrapper class to provide asynchronous I/O for the RoaminSMPP library.  Note that most 
	/// SMPP events have default handlers.  If the events are overridden by the caller by adding 
	/// event handlers, it is the caller's responsibility to ensure that the proper response is 
	/// sent.  For example: there is a default deliver_sm_resp implemented.  If you "listen" to 
	/// the deliver_sm event, it is your responsibility to then send the deliver_sm_resp packet.
	/// </summary>
	public class SMPPCommunicator : Component, IDisposable
	{
		private static readonly global::Common.Logging.ILog _Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		#region Inner classes
		private class RequestState 
		{
			public readonly uint SequenceNumber;
			public readonly ManualResetEvent EventHandler;
			public SmppResponse Response { get; set; }

			public RequestState(uint seqno)
			{
				SequenceNumber = seqno;
				EventHandler = new ManualResetEvent(false);
				Response = null;
			}
		}
		#endregion

		private readonly object _bindingLock = new object();

		private AsyncSocketClient asClient;
		private string _Host;
		private UInt16 _Port;
		private string _SystemId;
		private string _Password;
		private string _SystemType;
		private SmppBind.BindingType _BindType;
		private Pdu.NpiType _NpiType;
		private Pdu.TonType _TonType; 
		private SmppBind.SmppVersionType _Version;
		private string _AddressRange;
		private int _EnquireLinkInterval;
		private System.Timers.Timer _EnquireLinkTimer;
		private int _ResponseTimeout;		
		private int _ReBindInterval;
		private System.Timers.Timer _ReBindTimer;
		private bool _ReBindRequired = true; // True until first successfull bind.
		private bool _Bound;
		private bool _SentUnbindPacket = true;  //default to true since we start out unbound
		private Random _random = new Random();
		private uint _SequenceNumber = 0;
		private IDictionary<uint, RequestState> _RequestsAwaitingResponse = new Dictionary<uint, RequestState>();
		private SslProtocols _supportedSslProtocols;
		private bool _disableSslRevocationChecking;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		protected Container components = null;

		#region properties
		/// <summary>
		/// The host to bind this SMPPCommunicator to.
		/// </summary>
		public string Host
		{
			get
			{
				return _Host;
			}
			set
			{
				_Host = value;
			}
		}
		/// <summary>
		/// The port on the SMSC to connect to.
		/// </summary>
		public UInt16 Port
		{
			get
			{
				return _Port;
			}
			set
			{
				_Port = value;
			}
		}
		/// <summary>
		/// Accessor to determine if we have sent the unbind packet out.  Once the packet is 
		/// sent, you can consider this object to be unbound.
		/// </summary>
		public bool SentUnbindPacket
		{
			get
			{
				return _SentUnbindPacket;
			}
		}
		/// <summary>
		/// The binding type(receiver, transmitter, or transceiver)to use 
		/// when connecting to the SMSC.
		/// </summary>
		public SmppBind.BindingType BindType
		{
			get
			{
				return _BindType;
			}
			set
			{
				_BindType = value;
			}
		
		}
		/// <summary>
		/// The system type to use when connecting to the SMSC.
		/// </summary>
		public string SystemType
		{
			get
			{
				return _SystemType;
			}
			set
			{
				_SystemType = value;
			}
		}
		/// <summary>
		/// The system ID to use when connecting to the SMSC.  This is, 
		/// in essence, a user name.
		/// </summary>
		public string SystemId
		{
			get
			{
				return _SystemId;
			}
			set
			{
				_SystemId = value;
			}
		}
		/// <summary>
		/// The password to use when connecting to an SMSC.
		/// </summary>
		public string Password
		{
			get
			{
				return _Password;
			}
			set
			{
				_Password = value;
			}
		}
		/// <summary>
		/// The number plan indicator that this SMPPCommunicator should use.  
		/// </summary>
		public Pdu.NpiType NpiType 
		{
			get
			{
				return _NpiType;
			}
			set 
			{
				_NpiType = value;
			}
		}
		/// <summary>
		/// The type of number that this SMPPCommunicator should use.  
		/// </summary>
		public Pdu.TonType TonType 
		{
			get
			{
				return _TonType;
			}
			set 
			{
				_TonType = value;
			}
		}
		/// <summary>
		/// The SMPP specification version to use.
		/// </summary>
		public SmppBind.SmppVersionType Version 
		{
			get
			{
				return _Version;
			}
			set 
			{
				_Version = value;
			}
		}
		/// <summary>
		/// The address range of this SMPPCommunicator.
		/// </summary>
		public string AddressRange 
		{
			get
			{
				return _AddressRange;
			}
			set 
			{
				_AddressRange = value;
			}
		}
		/// <summary>
		/// Set to the number of seconds that should elapse in between enquire_link 
		/// packets.  Setting this to anything other than 0 will enable the timer, setting 
		/// it to 0 will disable the timer.  Note that the timer is only started/stopped 
		/// during a bind/unbind.  Negative values are ignored.
		/// </summary>
		public int EnquireLinkInterval
		{
			get 
			{
				return _EnquireLinkInterval;
			}

			set
			{
				if(value >= 0)
					_EnquireLinkInterval = value;
			}
		}
		/// <summary>
		/// Sets the number of seconds that the system will wait before trying to rebind 
		/// after a total network failure(due to cable problems, etc).  Negative values are 
		/// ignored, and 0 disables Re-Binding.
		/// </summary>
		public int ReBindInterval
		{
			get 
			{
				return _ReBindInterval;
			}

			set
			{
				if(value >= 0)
					_ReBindInterval = value;
			}
		}
		/// <summary>
		/// Gets or sets the response timeout (in miliseconds)
		/// </summary>
		/// <value>The response timeout.</value>
		public int ResponseTimeout
		{
			get { return _ResponseTimeout; }
			set { _ResponseTimeout = value; }
		}
		/// <summary>
		/// Gets a value indicating whether this <see cref="SMPPCommunicator"/> is connected.
		/// </summary>
		/// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
		public bool Connected { get { return asClient != null && asClient.Connected; } }
		/// <summary>
		/// Gets a value indicating whether this <see cref="SMPPCommunicator"/> is bound.
		/// </summary>
		/// <value><c>true</c> if bound; otherwise, <c>false</c>.</value>
		public bool Bound { get { lock (_bindingLock) return _Bound; } }
		public SslProtocols SupportedSslProtocols
		{
			get
			{
				return _supportedSslProtocols;
			}
			set
			{
				_supportedSslProtocols = value;
			}
		}

		/// <summary>
		/// Gets or sets if must check servers certificate revocation status.
		/// </summary>
		public bool DisableSslRevocationChecking
		{
			get
			{
				return _disableSslRevocationChecking;
			}
			set
			{
				_disableSslRevocationChecking = value;
			}
		}
		#endregion
		
		#region events
		/// <summary>
		/// Event called when the communicator receives a bind response.
		/// </summary>
		public event BindRespEventHandler OnBindResp;
		/// <summary>
		/// Event called when an error occurs.
		/// </summary>
		public event ErrorEventHandler OnError;
		/// <summary>
		/// Event called when the communicator is unbound.
		/// </summary>
		public event UnbindRespEventHandler OnUnboundResp;
		/// <summary>
		/// Event called when the connection is closed.
		/// </summary>
		public event ClosingEventHandler OnClose;
		/// <summary>
		/// Event called when an alert_notification comes in.
		/// </summary>
		public event AlertEventHandler OnAlert;
		/// <summary>
		/// Event called when a submit_sm_resp is received.
		/// </summary>
		public event SubmitSmRespEventHandler OnSubmitSmResp;
		/// <summary>
		/// Event called when a response to an enquire_link_resp is received.
		/// </summary>
		public event EnquireLinkRespEventHandler OnEnquireLinkResp;
		/// <summary>
		/// Event called when a submit_sm is received.
		/// </summary>
		public event SubmitSmEventHandler OnSubmitSm;
		/// <summary>
		/// Event called when a query_sm is received.
		/// </summary>
		public event QuerySmEventHandler OnQuerySm;
		/// <summary>
		/// Event called when a generic_nack is received.
		/// </summary>
		public event GenericNackEventHandler OnGenericNack;
		/// <summary>
		/// Event called when an enquire_link is received.
		/// </summary>
		public event EnquireLinkEventHandler OnEnquireLink;
		/// <summary>
		/// Event called when an unbind is received.
		/// </summary>
		public event UnbindEventHandler OnUnbind;
		/// <summary>
		/// Event called when the communicator receives a request for a bind.
		/// </summary>
		public event BindEventHandler OnBind;
		/// <summary>
		/// Event called when the communicator receives a cancel_sm.
		/// </summary>
		public event CancelSmEventHandler OnCancelSm;
		/// <summary>
		/// Event called when the communicator receives a cancel_sm_resp.
		/// </summary>
		public event CancelSmRespEventHandler OnCancelSmResp;
		/// <summary>
		/// Event called when the communicator receives a query_sm_resp.
		/// </summary>
		public event QuerySmRespEventHandler OnQuerySmResp;
		/// <summary>
		/// Event called when the communicator receives a data_sm.
		/// </summary>
		public event DataSmEventHandler OnDataSm;
		/// <summary>
		/// Event called when the communicator receives a data_sm_resp.
		/// </summary>
		public event DataSmRespEventHandler OnDataSmResp;
		/// <summary>
		/// Event called when the communicator receives a deliver_sm.
		/// </summary>
		public event DeliverSmEventHandler OnDeliverSm;
		/// <summary>
		/// Event called when the communicator receives a deliver_sm_resp.
		/// </summary>
		public event DeliverSmRespEventHandler OnDeliverSmResp;
		/// <summary>
		/// Event called when the communicator receives a replace_sm.
		/// </summary>
		public event ReplaceSmEventHandler OnReplaceSm;
		/// <summary>
		/// Event called when the communicator receives a replace_sm_resp.
		/// </summary>
		public event ReplaceSmRespEventHandler OnReplaceSmResp;
		/// <summary>
		/// Event called when the communicator receives a submit_multi.
		/// </summary>
		public event SubmitMultiEventHandler OnSubmitMulti;
		/// <summary>
		/// Event called when the communicator receives a submit_multi_resp.
		/// </summary>
		public event SubmitMultiRespEventHandler OnSubmitMultiResp;
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
	
		#endregion delegates

		#region dispatchers
		private void DispatchOnError(SmppExceptionEventArgs e)
		{
			if (OnError != null)
			{
				try
				{
					OnError(this, e);
				}
				catch (Exception ex)
				{
					_Log.Error("Unhandled exception thrown OnError event handler.", ex);
				}
			}
		}
		private void DispatchOnClose(EventArgs e)
		{
			//fire off a closing event
			if (OnClose != null)
			{
				try
				{
					OnClose(this, e);
				}
				catch (Exception ex)
				{
					_Log.Error("Unhandled exception thrown from OnClose handler.", ex);
				}
			}
		}
		#endregion

		#region constructors
		/// <summary>
		/// Creates a default SMPPCommunicator, with port 9999, bindtype set to 
		/// transceiver, host set to localhost, NPI type set to ISDN, TON type 
		/// set to International, version set to 3.4, enquire link interval set 
		/// to 0(disabled), sleep time after socket failure set to 10 seconds, 
		/// and address range, password, system type and system ID set to null 
		///(no value).
		/// </summary>
		/// <param name="container">The container that will hold this 
		/// component.</param>
		public SMPPCommunicator(IContainer container)
		{
			// Required for Windows.Forms Class Composition Designer support
			InitCommunicator();
			if (container != null) container.Add(this);
			InitializeComponent();
		}

		/// <summary>
		/// Creates a default SMPPCommunicator, with port 9999, bindtype set to 
		/// transceiver, host set to localhost, NPI type set to ISDN, TON type 
		/// set to International, version set to 3.4, enquire link interval set 
		/// to 0(disabled), sleep time after socket failure set to 10 seconds, 
		/// and address range, password, system type and system ID set to null 
		///(no value).
		/// </summary>
		public SMPPCommunicator()
			: this(null)
		{
		}
		#endregion constructors

		/// <summary>
		/// Sends a user-specified Pdu(see the RoaminSMPP base library for
		/// Pdu types).  This allows complete flexibility for sending Pdus.
		/// </summary>
		/// <param name="packet">The Pdu to send.</param>
		/// <returns>The sequence number of the sent PDU, or null if failed.</returns>
		public uint SendPdu(Pdu packet)
		{
			_Log.DebugFormat("Sending PDU: {0}", packet);

			if (packet.SequenceNumber == 0)
				packet.SequenceNumber = GenerateSequenceNumber();

			var bytes = packet.GetEncodedPdu();

			try
			{
				if (asClient == null || !asClient.Connected)
					throw new InvalidOperationException("Session not connected to remote party.");

				if (!(packet is SmppBind) && !_Bound)
					throw new InvalidOperationException("Session not bound to remote party.");

				asClient.Send(bytes);
				return packet.SequenceNumber;
			}
			catch
			{
				lock (_bindingLock)
				{
					_Log.Debug("SendPdu failed, scheduling a re-bind operation.");
					_ReBindRequired = true;
				}

				throw; // Let the exception flow..
			}
		}

		/// <summary>
		/// Sends a request and waits for the appropiate response.
		/// If no response is received before RequestTimeout seconds, an 
		/// SmppTimeoutException is thrown.
		/// </summary>
		/// <param name="request">The request.</param>
		public SmppResponse SendRequest(SmppRequest request)
		{
			RequestState state;

			lock (_RequestsAwaitingResponse)
			{
				state = new RequestState(SendPdu(request));
				_RequestsAwaitingResponse.Add(state.SequenceNumber, state);
			}

			var signalled = state.EventHandler.WaitOne(_ResponseTimeout);

			lock (_RequestsAwaitingResponse)
			{
				_RequestsAwaitingResponse.Remove(state.SequenceNumber);

				if (signalled)
				{
					return state.Response;
				} else {
					throw new SmppTimeoutException("Timeout while waiting for a response from remote side.");
				}
			}
		}

		public IEnumerable<SmppResponse> SendRequests(IEnumerable<SmppRequest> requests)
		{
			bool signalled = false;
			var list = new List<RequestState>();

			lock (_RequestsAwaitingResponse)
			{
				foreach (var request in requests)
					list.Add(new RequestState(SendPdu(request)));
			
				foreach (var state in list)
					_RequestsAwaitingResponse.Add(state.SequenceNumber, state);
			}

			var handlers = list.Select(x => x.EventHandler).ToArray();

			// WaitAll for multiple handles on an STA thread is not supported.
			// ...so wait on each handle individually.
			if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
			{
				var count = 0;

				_Log.Debug("Running under STA threads, waiting for SMPP responses using WaitAny workaround.");

				// FIXME: This has a worse case scenario which causes a timeout to last 
				//		  N*_ResponseTimeout. (pruiz)
				foreach (var handler in handlers)
				{
					if (WaitHandle.WaitAny(new[] { handler }, _ResponseTimeout) == 258)
					{
						break;
					}
					else
					{
						count++;
					}
				}

				// Signal received signal from all handlers??
				signalled = count == list.Count;
			}
			else
			{
				signalled = WaitHandle.WaitAll(handlers, _ResponseTimeout*handlers.Length);
			}

			lock (_RequestsAwaitingResponse)
			{
				foreach (var state in list)
					_RequestsAwaitingResponse.Remove(state.SequenceNumber);

				if (signalled)
				{
					return list.Select(x => x.Response);
				}
				else
				{
					throw new SmppTimeoutException(string.Format(
						"Timeout while waiting for a responses from remote side. (Missing: {0}/{1})",
						list.Count(x => x.Response == null), list.Count()
					));
				}
			}
		}

		/// <summary>
		/// Sends an SMS message synchronouslly, possibly splitting it on multiple PDUs 
		/// using the specified segmentation & reassembly method.
		/// </summary>
		/// <param name="pdu">The pdu.</param>
		/// <param name="method">The method.</param>
		/// <returns>The list of messageIds assigned by remote party to each submitted PDU.</returns>
		public IEnumerable<string> Send(SmppSubmitSm pdu, SmppSarMethod method)
		{
			var requests = SmppUtil.SplitLongMessage(pdu, method, GetRandomByte()).Cast<SmppRequest>();
			var responses = SendRequests(requests);

			if (responses.Any(x => (x is SmppGenericNackResp)))
			{
				var nack = responses.First(x => x is SmppGenericNackResp);
				var idx = responses.IndexWhere(x => x == nack);
				var req = requests.ElementAt(idx);
				var msg = string.Format("SMPP PDU was rejected by remote party. (error: {0})", nack.CommandStatus);
				throw new SmppRequestException(msg, req, nack);
			}

			if (responses.Any(x => x.CommandStatus != 0))
			{
				var res = responses.First(x => x.CommandStatus != 0);
				var idx = responses.IndexWhere(x => x == res);
				var req = requests.ElementAt(idx);
				var msg = string.Format("SMPP Request returned an error status. (code: {0})", res.CommandStatus);
				throw new SmppRequestException(msg, req, res);
			}

			return responses.OfType<SmppSubmitSmResp>().Select(x => x.MessageId).ToArray();
		}

		/// <summary>
		/// Connects and binds the SMPPCommunicator to the SMSC, using the
		/// values that have been set in the constructor and through the
		/// properties.  This will also start the timers that at regular intervals
		/// send enquire_link packets and the one which re-binds the session, if their
		/// interval properties have a non-zero value.
		/// </summary>
		/// <returns>
		/// True if bind was successfull or false if connection/bind failed 
		/// (and will be retried later upon ReBindInterval)
		/// </returns>
		public bool Bind()
		{
			lock (_bindingLock)
			{
				_Log.DebugFormat("Binding to {0}:{1}.", Host, Port);

				if (_Bound)
					throw new InvalidOperationException("Already bound to remote party, unbind session first!");

				_ReBindTimer.Stop(); // (temporarilly) disable re-binding timer.

				try
				{
					if (asClient != null)
					{
						var tmp = asClient;
						asClient = null;
						tmp.Dispose();
					}
				}
				catch
				{
					//drop it on the floor
				}

				//connect
				try
				{
					asClient = new AsyncSocketClient(10240, null,
						new AsyncSocketClient.MessageHandler(ClientMessageHandler),
						new AsyncSocketClient.SocketClosingHandler(ClientCloseHandler),
						new AsyncSocketClient.ErrorHandler(ClientErrorHandler));

					asClient.Connect(Host, Port, SupportedSslProtocols, DisableSslRevocationChecking);

					// re-initialize seq. numbers.
					lock (this) _SequenceNumber = 1; 
					// SequenceNumbers are per-session, so reset waiting list..
					lock (_RequestsAwaitingResponse) _RequestsAwaitingResponse.Clear();

					SmppBind request = new SmppBind();
					request.SystemId = SystemId;
					request.Password = Password;
					request.SystemType = SystemType;
					request.InterfaceVersion = Version;
					request.AddressTon = TonType;
					request.AddressNpi = NpiType;
					request.AddressRange = AddressRange;
					request.BindType = BindType;

					var response = SendRequest(request);

					if (response.CommandStatus != 0)
						throw new SmppRequestException("Bind request failed.", response.CommandStatus);

					_SentUnbindPacket = false;

					// Enable/Disable enquire timer.
					_EnquireLinkTimer.Stop();

					if (_EnquireLinkInterval > 0)
					{
						_EnquireLinkTimer.Interval = _EnquireLinkInterval * 1000;
						_EnquireLinkTimer.Start();
					}

					_Bound = true;
					_ReBindRequired = false;

					return true;
				}
				catch (Exception exc)
				{
					DispatchOnError(new SmppExceptionEventArgs(exc));
				}
				finally
				{
					// Re-enable rebinding timer..

					if (_ReBindInterval > 0)
					{
						_ReBindTimer.Interval = _ReBindInterval * 1000;
						_ReBindTimer.Start();
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Unbinds the SMPPCommunicator from the SMSC then disconnects the socket
		/// when it receives the unbind response from the SMSC.  This will also stop the 
		/// timer that sends out the enquire_link packets if it has been enabled.  You need to 
		/// explicitly call this to unbind.; it will not be done for you.
		/// </summary>
		public void Unbind()
		{
			Unbind(true);
		}

		#region internal methods
		/// <summary>
		/// Callback method to handle received messages.  The AsyncSocketClient
		/// library calls this; don't call it yourself.
		/// </summary>
		/// <param name="client">The client to receive messages from.</param>
		internal void ClientMessageHandler(AsyncSocketClient client)
		{
			try
			{
				// OPTIMIZE: Use a single PduFactory instance, instead of a new one each time.
				Queue responseQueue = new PduFactory().GetPduQueue(client.Buffer);
				ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessPduQueue), responseQueue);
			}
			catch (Exception exception)
			{
				DispatchOnError(new SmppExceptionEventArgs(exception));
			}
		}

		/// <summary>
		/// Callback method to handle socket closing.
		/// </summary>
		/// <param name="client">The client to receive messages from.</param>
		internal void ClientCloseHandler(AsyncSocketClient client)
		{
			lock (_bindingLock)
			{
				_Log.Warn("Socket closed, scheduling a rebind operation.");
				_ReBindRequired = true;
			}

			DispatchOnClose(new EventArgs());
		}

		/// <summary>
		/// Callback method to handle errors.
		/// </summary>
		/// <param name="client">The client to receive messages from.</param>
		/// <param name="exception">The generated exception.</param>
		internal void ClientErrorHandler(AsyncSocketClient client, Exception exception)
		{
			DispatchOnError(new SmppExceptionEventArgs(exception));
		}
		#endregion internal methods

		#region private methods
		/// <summary>
		/// Generates a monotonically increasing sequence number for each Pdu.  When it
		/// hits the the 32 bit unsigned int maximum, it starts over.
		/// </summary>
		private uint GenerateSequenceNumber()
		{
			lock (this)
			{
				_SequenceNumber++;
				if (_SequenceNumber >= UInt32.MaxValue)
				{
					_SequenceNumber = 1;
				}
				return _SequenceNumber;
			}
		}

		/// <summary>
		/// Gets a random byte.
		/// </summary>
		/// <returns></returns>
		private byte GetRandomByte()
		{
			lock (_random)
			{
				return Convert.ToByte(_random.Next(254));
			}
		}

		private void Unbind(bool disableReBind)
		{
			lock (_bindingLock)
			{
				_ReBindTimer.Enabled = !disableReBind;

				if (_EnquireLinkTimer.Enabled)
					_EnquireLinkTimer.Stop();

				if (!_SentUnbindPacket)
				{
					Helper.ShallowExceptions(() => SendPdu(new SmppUnbind()));
					_SentUnbindPacket = true;
				}

				_Bound = false;
			}
		}

		/// <summary>
		/// Goes through the packets in the queue and fires events for them.  Called by the
		/// threads in the ThreadPool.
		/// </summary>
		/// <param name="queueStateObj">The queue of byte packets.</param>
		private void ProcessPduQueue(object queueStateObj)
		{
			foreach(Pdu packet in (queueStateObj as Queue))
			{
				if (packet == null) continue;

				_Log.DebugFormat("Recived PDU: {0}", packet);

				try
				{
					// Handle packets related to a request awaiting response.
					if (packet is SmppResponse || packet is SmppGenericNack)
					{
						lock (_RequestsAwaitingResponse)
						{
							if (_RequestsAwaitingResponse.ContainsKey(packet.SequenceNumber))
							{
								var state = _RequestsAwaitingResponse[packet.SequenceNumber];

								// Save response at bucket..
								state.Response = packet is SmppGenericNack ? 
                                    new SmppGenericNackResp(packet.PacketBytes) : packet as SmppResponse;
								// Signal response reception..
								state.EventHandler.Set();
								continue;
							}
						}
					}

					// based on each Pdu, fire off an event
					FireEvents(packet);
				}
				catch (Exception exception)
				{
					DispatchOnError(new SmppExceptionEventArgs(exception));
				}
			}
		}
		
		/// <summary>
		/// Sends out an enquire_link packet.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="ea"></param>
		private void EnquireLinkTimerElapsed(object sender, ElapsedEventArgs ea)
		{
			bool locked = false;

			if (!_Bound)
			{
				_Log.Warn("Cannot send enquire request over an unbound session!");
				return;
			}

			try
			{
				locked = Monitor.TryEnter(_EnquireLinkTimer);

				if (!locked) return;

				SendPdu(new SmppEnquireLink());
			}
			finally
			{
				if (locked) Monitor.Exit(_EnquireLinkTimer);
			}
		}
		/// <summary>
		/// Performs a re-bind if current connection was lost.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="ea">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
		private void ReBindTimerElapsed(object sender, ElapsedEventArgs ea)
		{
			var locked = false;

			try
			{
				locked = Monitor.TryEnter(_bindingLock);

				if (!locked || !_ReBindRequired)
					return;

				_Log.Debug("Rebinding..");

				try
				{
					Unbind(false);
				}
				catch { }

				try
				{
					Bind();
				}
				catch { }
			}
			finally
			{
				if (locked) Monitor.Exit(_bindingLock);
			}
		}

		/// <summary>
		/// Fires an event off based on what Pdu is received in.
		/// </summary>
		/// <param name="response">The response to fire an event for.</param>
		private void FireEvents(Pdu response)
		{
			//here we go...
			if(response is SmppBindResp)
			{
				if(OnBindResp != null)
				{
					OnBindResp(this, new BindRespEventArgs((SmppBindResp)response));
				}
			} 
			else if(response is SmppUnbindResp)
			{
				//disconnect
				asClient.Disconnect();
				if(OnUnboundResp != null)
				{
					OnUnboundResp(this, new UnbindRespEventArgs((SmppUnbindResp)response));
				}
			} 
			else if(response is SmppAlertNotification)
			{
				if(OnAlert != null)
				{
					OnAlert(this, new AlertEventArgs((SmppAlertNotification)response));
				}
			}	
			else if(response is SmppSubmitSmResp)
			{
				if(OnSubmitSmResp != null)
				{
					OnSubmitSmResp(this,
						new SubmitSmRespEventArgs((SmppSubmitSmResp)response));
				}
			}
			else if(response is SmppEnquireLinkResp)
			{
				if(OnEnquireLinkResp != null)
				{
					OnEnquireLinkResp(this, new EnquireLinkRespEventArgs((SmppEnquireLinkResp)response));
				}
			}
			else if(response is SmppSubmitSm)
			{
				if(OnSubmitSm != null)
				{
					OnSubmitSm(this, new SubmitSmEventArgs((SmppSubmitSm)response));
				}
				else
				{
					//default a response
					SmppSubmitSmResp pdu = new SmppSubmitSmResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.MessageId = System.Guid.NewGuid().ToString().Substring(0, 10);
					pdu.CommandStatus = 0;
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppQuerySm)
			{
				if(OnQuerySm != null)
				{
					OnQuerySm(this, new QuerySmEventArgs((SmppQuerySm)response));
				}
				else
				{
					//default a response
					SmppQuerySmResp pdu = new SmppQuerySmResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.CommandStatus = 0;
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppGenericNack)
			{
				if(OnGenericNack != null)
				{
					OnGenericNack(this, new GenericNackEventArgs((SmppGenericNack)response));
				}
			}
			else if(response is SmppEnquireLink)
			{
				if(OnEnquireLink != null)
				{
					OnEnquireLink(this, new EnquireLinkEventArgs((SmppEnquireLink)response));
				}
				
				//send a response back
				SmppEnquireLinkResp pdu = new SmppEnquireLinkResp();
				pdu.SequenceNumber = response.SequenceNumber;
				pdu.CommandStatus = 0;

				SendPdu(pdu);
			}
			else if(response is SmppUnbind)
			{
				if(OnUnbind != null)
				{
					OnUnbind(this, new UnbindEventArgs((SmppUnbind)response));
				}
				else
				{
					//default a response
					SmppUnbindResp pdu = new SmppUnbindResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.CommandStatus = 0;
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppBind)
			{
				if(OnBind != null)
				{
					OnBind(this, new BindEventArgs((SmppBind)response));
				}
				else
				{
					//default a response
					SmppBindResp pdu = new SmppBindResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.CommandStatus = 0;
					pdu.SystemId = "Generic";
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppCancelSm)
			{
				if(OnCancelSm != null)
				{
					OnCancelSm(this, new CancelSmEventArgs((SmppCancelSm)response));
				}
				else
				{
					//default a response
					SmppCancelSmResp pdu = new SmppCancelSmResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.CommandStatus = 0;
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppCancelSmResp)
			{
				if(OnCancelSmResp != null)
				{
					OnCancelSmResp(this, new CancelSmRespEventArgs((SmppCancelSmResp)response));
				}
			}
			else if(response is SmppCancelSmResp)
			{
				if(OnCancelSmResp != null)
				{
					OnCancelSmResp(this, new CancelSmRespEventArgs((SmppCancelSmResp)response));
				}
			}
			else if(response is SmppQuerySmResp)
			{
				if(OnQuerySmResp != null)
				{
					OnQuerySmResp(this, new QuerySmRespEventArgs((SmppQuerySmResp)response));
				}
			}
			else if(response is SmppDataSm)
			{
				if(OnDataSm != null)
				{
					OnDataSm(this, new DataSmEventArgs((SmppDataSm)response));
				}
				else
				{
					//default a response
					SmppDataSmResp pdu = new SmppDataSmResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.CommandStatus = 0;
					pdu.MessageId = "Generic";
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppDataSmResp)
			{
				if(OnDataSmResp != null)
				{
					OnDataSmResp(this, new DataSmRespEventArgs((SmppDataSmResp)response));
				}
			}
			else if(response is SmppDeliverSm)
			{
				if(OnDeliverSm != null)
				{
					OnDeliverSm(this, new DeliverSmEventArgs((SmppDeliverSm)response));
				}
				else
				{
					//default a response
					SmppDeliverSmResp pdu = new SmppDeliverSmResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.CommandStatus = 0;
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppDeliverSmResp)
			{
				if(OnDeliverSmResp != null)
				{
					OnDeliverSmResp(this, new DeliverSmRespEventArgs((SmppDeliverSmResp)response));
				}
			}
			else if(response is SmppReplaceSm)
			{
				if(OnReplaceSm != null)
				{
					OnReplaceSm(this, new ReplaceSmEventArgs((SmppReplaceSm)response));
				}
				else
				{
					//default a response
					SmppReplaceSmResp pdu = new SmppReplaceSmResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.CommandStatus = 0;
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppReplaceSmResp)
			{
				if(OnReplaceSmResp != null)
				{
					OnReplaceSmResp(this, new ReplaceSmRespEventArgs((SmppReplaceSmResp)response));
				}
			}
			else if(response is SmppSubmitMulti)
			{
				if(OnSubmitMulti != null)
				{
					OnSubmitMulti(this, new SubmitMultiEventArgs((SmppSubmitMulti)response));
				}
				else
				{
					//default a response
					SmppSubmitMultiResp pdu = new SmppSubmitMultiResp();
					pdu.SequenceNumber = response.SequenceNumber;
					pdu.CommandStatus = 0;
	
					SendPdu(pdu);
				}
			}
			else if(response is SmppSubmitMultiResp)
			{
				if(OnSubmitMultiResp != null)
				{
					OnSubmitMultiResp(this, new SubmitMultiRespEventArgs((SmppSubmitMultiResp)response));
				}
			}
		}
	
		/// <summary>
		/// Initializes the SMPPCommunicator with some default values.
		/// </summary>
		private void InitCommunicator()
		{		
			Port = 9999;
			BindType = SmppBind.BindingType.BindAsTransceiver;
			Host = "localhost";
			NpiType = Pdu.NpiType.ISDN;
			TonType = Pdu.TonType.International; 
			Version = SmppBind.SmppVersionType.Version3_4;
			AddressRange = null;
			Password = null;
			SystemId = null;
			SystemType = null;
			EnquireLinkInterval = 0;
			ReBindInterval = 10;
			ResponseTimeout = 2500;

			// Initialize timers..
			_EnquireLinkTimer = new System.Timers.Timer() { Enabled = false };
			_EnquireLinkTimer.Elapsed += new ElapsedEventHandler(EnquireLinkTimerElapsed);
			_ReBindTimer = new System.Timers.Timer() { Enabled = false };
			_ReBindTimer.Elapsed += new ElapsedEventHandler(ReBindTimerElapsed);
		}
		#endregion private methods
		
		#region Component Designer generated code
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		protected void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
		
		/// <summary>
		/// Disposes of this component.  Called by the framework; do not call it 
		/// directly.
		/// </summary>
		/// <param name="disposing">This is set to false during garbage collection but 
		/// true during a disposal.</param>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
			}

			Helper.ShallowExceptions(() => Unbind(true));
			Helper.ShallowExceptions(() => { if (asClient != null) asClient.Dispose(); });

			lock (_RequestsAwaitingResponse)
			{
				_RequestsAwaitingResponse.Clear();
				_RequestsAwaitingResponse = null;
			}

			base.Dispose(disposing);
		}
	}
}
