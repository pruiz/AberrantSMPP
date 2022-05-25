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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Authentication;

using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Common.Internal.Logging;

using Microsoft.Extensions.Logging;

using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;
using AberrantSMPP.Exceptions;
using AberrantSMPP.EventObjects;
using AberrantSMPP.Utility;
using Dawn;

namespace AberrantSMPP 
{
	/// <summary>
	/// Wrapper class to provide asynchronous I/O for the RoaminSMPP library.  Note that most 
	/// SMPP events have default handlers.  If the events are overridden by the caller by adding 
	/// event handlers, it is the caller's responsibility to ensure that the proper response is 
	/// sent.  For example: there is a default deliver_sm_resp implemented.  If you "listen" to 
	/// the deliver_sm event, it is your responsibility to then send the deliver_sm_resp packet.
	/// </summary>
	public partial class SMPPClient : IDisposable
	{
		private static readonly global::Common.Logging.ILog _Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly object _bindingLock = new object();
		// XXX: Beware of the implications of using ReaderWriterLockSlim with Async/Await code. (pruiz)
		private readonly ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		private CancellationTokenSource _cancellator = new CancellationTokenSource();

		private Bootstrap _channelFactory;
		private IEventLoopGroup _eventLoopGroup;
		private IChannel _channel;
		private States _state = States.Inactive;
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
		private Random _random = new Random();
		private uint _SequenceNumber = 0;
		private uint _channelBufferSize = 10240;
		private IDictionary<uint, RequestState> _RequestsAwaitingResponse = new Dictionary<uint, RequestState>();
		private SslProtocols _supportedSslProtocols;

		#region properties

		/// <summary>
		/// The host to bind this SMPPCommunicator to.
		/// </summary>
		public string Host { get; private set; } = "127.0.0.1";

		/// <summary>
		/// The port on the SMSC to connect to.
		/// </summary>
		public UInt16 Port { get; private set; } = 2775;
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
		
		public States State 
		{
			get
			{
				using (_stateLock.ForReadOnly()) return _state;
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
		public delegate void ErrorEventHandler(object source, CommonErrorEventArgs e);
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
		private void DispatchOnError(CommonErrorEventArgs e)
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

		public class OptionsMonitor<T> : Microsoft.Extensions.Options.IOptionsMonitor<T>
		{
			private readonly T options;

			public OptionsMonitor(T options)
			{
				this.options = options;
			}

			public T CurrentValue => options;

			public T Get(string name) => options;

			public IDisposable OnChange(Action<T, string> listener) => new NullDisposable();

			private class NullDisposable : IDisposable
			{
				public void Dispose() { }
			}
		}
		
		static SMPPClient()
		{
			var options = new Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions();
			InternalLoggerFactory.DefaultFactory.AddProvider(new Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider(new OptionsMonitor<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions>(options)));
		}

		/// <summary>
		/// Creates a default SMPPClient, with port 9999, bindtype set to 
		/// transceiver, host set to localhost, NPI type set to ISDN, TON type 
		/// set to International, version set to 3.4, enquire link interval set 
		/// to 0(disabled), sleep time after socket failure set to 10 seconds, 
		/// and address range, password, system type and system ID set to null 
		///(no value).
		/// </summary>
		public SMPPClient(string host, ushort port) //< FIXME: No longer a component, so: pass parameters via .ctor
		{
			Host = host;
			Port = port;
			BindType = SmppBind.BindingType.BindAsTransceiver;
			NpiType = Pdu.NpiType.ISDN;
			TonType = Pdu.TonType.International; 
			Version = Pdu.SmppVersionType.Version3_4;
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

			_eventLoopGroup = new MultithreadEventLoopGroup();
			_channelFactory = new Bootstrap()
				.Group(_eventLoopGroup)
				.Channel<TcpSocketChannel>()
				.Option(ChannelOption.TcpNodelay, true)
				.Option(ChannelOption.SoLinger, 0)
				.Option(ChannelOption.SoRcvbuf, (int)_channelBufferSize)
				.Option(ChannelOption.SoSndbuf, (int)_channelBufferSize)
				.RemoteAddress(Host, Port)
				.Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
				{
					var pipeline = channel.Pipeline;

					if (_supportedSslProtocols != SslProtocols.None)
					{
						//pipeline.AddLast("tls", new TlsHandler(stream => new SslStream()))
					}

					pipeline
						.AddLast(new LoggingHandler())
						//.AddLast("framing-enc", new LengthFieldPrepender(4, true))
						.AddLast("framing-dec",
							new LengthFieldBasedFrameDecoder(ByteOrder.BigEndian, Int32.MaxValue, 0, 4, -4, 0, false))
						.AddLast("pdu-codec", new PduCodec())
						.AddLast("inbound-handler", new InboundHandler(this));
				}));
		}
		#endregion constructors

		// Starts SMPP client w/ background reconnecting / re-binding as needed.
		/// <returns>
		/// True if bind was successfull or false if connection/bind failed 
		/// (and will be retried later upon ReBindInterval)
		/// </returns>
		public void Start()
		{
			throw new NotImplementedException();
			
			_ReBindTimer.Stop(); // (temporarilly) disable re-binding timer.
			_EnquireLinkTimer.Stop(); // (temporarilly) disable enquire timer.
			
			//	finally
			{
				// Re-enable rebinding timer..
				if (_ReBindInterval > 0)
				{
					_ReBindTimer.Interval = _ReBindInterval * 1000;
					_ReBindTimer.Start();
				}
			}
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}
		
		public void Connect()
		{
			using (_stateLock.ForWrite())
			{
				Guard.Operation(_state is States.Inactive,
					$"Can't connect an a client w/ state {_state}, already connected?");

				_cancellator.Token.ThrowIfCancellationRequested();
				
				_Log.DebugFormat("Connecting to {0}:{1}.", Host, Port);
				// FIXME: Pass timeout & cancellator..
				_channel = _channelFactory.ConnectAsync()
					.WithCancellation(_cancellator.Token)
					.GetAwaiter().GetResult();

				_state = States.Connected;
			}
		}

		/// <summary>
		/// Connects and binds the SMPPClient to the SMSC, using the
		/// values that have been set in the constructor and through the
		/// properties.  This will also start the timers that at regular intervals
		/// send enquire_link packets and the one which re-binds the session, if their
		/// interval properties have a non-zero value.
		/// </summary>
		public void Bind()
		{
			using (_stateLock.ForWrite()) //< FIXME: Allow timing out..
			{
				_cancellator.Token.ThrowIfCancellationRequested();
				GuardEx.Against(_state == States.Bound, "Already bound to remote party, unbind session first.");
				GuardEx.Against(_state != States.Connected, "Can't bind non-connected session, call Connect() first.");
				GuardEx.Against(_EnquireLinkTimer.Enabled, "EnquireLinkTimer Enabled while binding?!");

				_Log.InfoFormat("Binding to {0}:{1}..", Host, Port);

				// re-initialize seq. numbers.
				InterlockedEx.Exchange(ref _SequenceNumber, 0);

				// SequenceNumbers are per-session, so reset waiting list..
				lock (_RequestsAwaitingResponse) _RequestsAwaitingResponse.Clear();

				var response = SendAndWait(new SmppBind()
				{
					SystemId = SystemId,
					Password = Password,
					SystemType = SystemType,
					InterfaceVersion = Version,
					AddressTon = TonType,
					AddressNpi = NpiType,
					AddressRange = AddressRange,
					BindType = BindType,
				});

				if (response.CommandStatus != 0)
					throw new SmppRequestException("Bind request failed.", response.CommandStatus);

				if (_EnquireLinkInterval > 0)
				{
					_EnquireLinkTimer.Interval = _EnquireLinkInterval * 1000;
					_EnquireLinkTimer.Start();
				}

				_state = States.Bound;
				
				_Log.InfoFormat("Bound to {0}:{1}.", Host, Port);
			}
		}

		/// <summary>
		/// Unbinds the SMPPClient from the SMSC when it receives the unbind response from the SMSC.
		/// This will also stop the timer that sends out the enquire_link packets if it has been enabled.
		/// You need to explicitly call this to unbind.; it will not be done for you.
		/// </summary>
		public void Unbind()
		{
			using (_stateLock.ForWrite()) //< FIXME: Allow timing out..
			{
				_cancellator.Token.ThrowIfCancellationRequested();
				Guard.Operation(_state == States.Bound, $"Can't unbind a session w/ state {_state}, try binding first.");
				
				_Log.InfoFormat("Unbinding from {0}:{1}..", Host, Port);

				if (_EnquireLinkTimer.Enabled)
					_EnquireLinkTimer.Stop();

				SendAndWait(new SmppUnbind());

				_state = States.Connected;
				
				_Log.InfoFormat("Unbound from {0}:{1}.", Host, Port);
			}
		}

		/// <summary>
		/// Sends a user-specified Pdu (see Packet namespace for
		/// Pdu types). This allows complete flexibility for sending Pdus.
		/// </summary>
		/// <param name="packet">The Pdu to send.</param>
		/// <returns>The sequence number of the sent PDU, or null if failed.</returns>
		public uint SendPdu(Pdu packet)
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			try
			{
				_Log.DebugFormat("Sending PDU: {0}", packet);

				using (_stateLock.ForReadOnly()) //< FIXME: Allow timing out..
				{
					if (packet.SequenceNumber == 0)
					{
						// Generate a monotonically increasing sequence number for each Pdu.
						// When it hits the the 32 bit unsigned int maximum, it starts over.
						packet.SequenceNumber = InterlockedEx.Increment(ref _SequenceNumber);
					}
					
					GuardEx.Against(_channel == null, "Channel has not been initialized?!");
					Guard.Operation(_channel?.Active == true, "Channel is not active?!.");
					GuardEx.Against(_state < States.Connected, "Session is not connected.");
					GuardEx.Against(!(packet is SmppBind) && _state != States.Bound, "Session not bound to remote party.");

					_channel!.WriteAndFlushAsync(packet).Wait(_cancellator.Token); //< FIXME: Allow timing out..
					return packet.SequenceNumber;
				}
			}
			catch (Exception ex)
			{
				_Log.Debug("SendPdu failed.", ex);
				throw; // Let the exception flow..
			}
		}

		/// <summary>
		/// Sends a request and waits for the appropriate response.
		/// If no response is received before RequestTimeout seconds, an 
		/// SmppTimeoutException is thrown.
		/// If a response is received w/ CommandStatus != OK, an
		/// SmppRequestException is thrown.
		/// </summary>
		/// <param name="request">The request.</param>
		public SmppResponse SendAndWait(SmppRequest request)
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
					return state.Response.CommandStatus == CommandStatus.ESME_ROK ? state.Response
						: throw new SmppRequestException("SmppRequest failed.", request, state.Response);
				}
				
				throw new SmppTimeoutException("Timeout while waiting for a response from remote side.");
			}
		}

		public IEnumerable<SmppResponse> SendAndWait(IEnumerable<SmppRequest> requests)
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
		public IEnumerable<string> SendAndWait(SmppSubmitSm pdu, SmppSarMethod method)
		{
			var requests = SmppUtil.SplitLongMessage(pdu, method, GetRandomByte()).Cast<SmppRequest>();
			var responses = SendAndWait(requests);

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
		/// Sends an SMS message synchronouslly, possibly splitting it on multiple PDUs 
		/// using the specified segmentation & reassembly method.
		/// </summary>
		/// <param name="pdu">The pdu.</param>
		/// <param name="method">The method.</param>
		/// <returns>The submitted PDUs, along with it's correlated response PDUs.</returns>
		public IDictionary<SmppSubmitSm, SmppSubmitSmResp> SendAndWaitEx(SmppSubmitSm pdu, SmppSarMethod method)
		{
			var requests = SmppUtil.SplitLongMessage(pdu, method, GetRandomByte()).Cast<SmppRequest>();
			var responses = SendAndWait(requests);

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

			return responses.OfType<SmppSubmitSmResp>()
				.Select(x => new
				{
					Request = requests.Single(r => r.SequenceNumber == x.SequenceNumber),
					Response = x
				})
				.ToDictionary(x => (SmppSubmitSm)x.Request, x => x.Response);
		}
		
		#region private methods

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
		
		private void ThrowIfStateIs(string operation, params States[] invalidStates)
		{
			using (_stateLock.ForReadOnly())
			{
				if (invalidStates.Contains((_state)))
				{
					throw new InvalidOperationException($"Invalid {operation} invoked while on state {_state}.");
				}
			}
		}
		
		private void ThrowIfStateIsNot(string operation, params States[] validStates)
		{
			using (_stateLock.ForReadOnly())
			{
				if (!validStates.Contains((_state)))
				{
					throw new InvalidOperationException($"Invalid {operation} invoked while on state {_state}.");
				}
			}
		}

		/// <summary>
		/// Goes through the packets in the queue and fires events for them.  Called by the
		/// threads in the ThreadPool.
		/// </summary>
		/// <param name="queueStateObj">The queue of byte packets.</param>
		private void ProcessPdu(Pdu packet)
		{
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
							state.Response = packet is SmppGenericNack
								? new SmppGenericNackResp(packet.PacketBytes)
								: packet as SmppResponse;
							// Signal response reception..
							state.EventHandler.Set();
						}
					}
				}

				// based on each Pdu, fire off an event
				FireEvents(packet);
			}
			catch (Exception exception)
			{
				DispatchOnError(new CommonErrorEventArgs(exception));
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

			try
			{
				using (_stateLock.ForReadOnly())
				{
					if (_state != States.Bound)
					{
						_Log.Warn("Cannot send enquire request over an unbound session!");
						return;
					}
				}

				locked = Monitor.TryEnter(_EnquireLinkTimer);

				if (!locked) return;

				SendPdu(new SmppEnquireLink());
			}
			catch (Exception ex)
			{
				_Log.Warn("Unexpected error while sending enquire link request.", ex);
				DispatchOnError(new CommonErrorEventArgs(ex));
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
				locked = _stateLock.TryEnterUpgradeableReadLock(0);

				Guard.Operation(_state is States.Invalid, $"Invalid State={_state} while trying to rebind.");
				
				if (!locked || _state is States.Binding or States.Bound)
					return;
				
				_Log.Info("Session not bound, trying to re-establish session..");

				try
				{
					Unbind();
				}
				catch
				{
				}
				
				if (_state is States.Inactive)
				{
					Connect();
				}
				
				Bind();
			}
			catch (Exception ex)
			{
				_Log.Warn("Unexpected error while trying to rebind.", ex);
				DispatchOnError(new CommonErrorEventArgs(ex));
			}
			finally
			{
				if (locked) _stateLock.ExitUpgradeableReadLock();
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
				_channel.DisconnectAsync().Wait();
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
			else if(response is SmppReplaceSmResp resp)
			{
				OnReplaceSmResp?.Invoke(this, new ReplaceSmRespEventArgs(resp));
			}
			else if(response is SmppSubmitMulti multi)
			{
				if(OnSubmitMulti != null)
				{
					OnSubmitMulti(this, new SubmitMultiEventArgs(multi));
				}
				else
				{
					//default a response
					SmppSubmitMultiResp pdu = new SmppSubmitMultiResp();
					pdu.SequenceNumber = multi.SequenceNumber;
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

		#endregion private methods

		/// <summary>
		/// Disposes of this component.  Called by the framework; do not call it 
		/// directly.
		/// </summary>
		public void Dispose()
		{
			_Log.DebugFormat("Disposing session for {0}:{1}", Host, Port);
			
			Helper.ShallowExceptions(() => Unbind());
			Helper.ShallowExceptions(() => { _channel?.CloseAsync().Wait(); });

			lock (_RequestsAwaitingResponse)
			{
				_RequestsAwaitingResponse.Clear();
				_RequestsAwaitingResponse = null;
			}
		}
	}
}
