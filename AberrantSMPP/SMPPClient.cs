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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Runtime.Remoting.Contexts;
using System.Reflection;

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
	public partial class SMPPClient : ISmppClient
	{
		private static readonly global::Common.Logging.ILog _Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly uint CHANNEL_BUFFER_SIZE = 10240;

		private readonly CancellationTokenSource _cancellator = new CancellationTokenSource();
		private readonly Random _random = new Random();
		private readonly object _lock = new object();
		private readonly IEventLoopGroup _eventLoopGroup;
		private Bootstrap _channelFactory;
		private IChannel _channel;
		private volatile States _state;
		private bool _started = false; //< Signals when we are using automatic connection handling (ie. Start/Stop)
		private Task _connecting = null;

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
		public SmppBind.BindingType BindType { get; set; } = SmppBind.BindingType.BindAsTransceiver;

		/// <summary>
		/// The system type to use when connecting to the SMSC.
		/// </summary>
		public string SystemType { get; set; }
		
		/// <summary>
		/// The system ID to use when connecting to the SMSC.  This is, 
		/// in essence, a user name.
		/// </summary>
		public string SystemId { get; set; }
		
		/// <summary>
		/// The password to use when connecting to an SMSC.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// The number plan indicator that this SMPPCommunicator should use.  
		/// </summary>
		public Pdu.NpiType NpiType { get; set; } = Pdu.NpiType.ISDN;

		/// <summary>
		/// The type of number that this SMPPCommunicator should use.  
		/// </summary>
		public Pdu.TonType TonType { get; set; } = Pdu.TonType.International;

		/// <summary>
		/// The SMPP specification version to use.
		/// </summary>
		public SmppBind.SmppVersionType Version { get; set; } = Pdu.SmppVersionType.Version3_4;
		
		/// <summary>
		/// The address range of this SMPPCommunicator.
		/// </summary>
		public string AddressRange  { get; set; }

		/// <summary>
		/// Gets or sets the connect timeout (in miliseconds)
		/// </summary>
		/// <value>The response timeout.</value>
		public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

		/// <summary>
		/// Gets or sets the disconnect timeout (in miliseconds)
		/// </summary>
		/// <value>The response timeout.</value>
		public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

		/// <summary>
		/// Gets or sets the socket timeout for network I/O operations.
		/// </summary>
		public TimeSpan SocketTimeout { get; set; } = TimeSpan.FromMilliseconds(30);

		/// <summary>
		/// Gets or sets the request timeout (in miliseconds)
		/// </summary>
		/// <value>The response timeout.</value>
		public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5); //< FIXME: Review if we are using this
		
		/// <summary>
		/// Gets or sets the response timeout (in miliseconds)
		/// </summary>
		/// <value>The response timeout.</value>
		public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(10);  //< FIXME: Review if we are using this

		/// <summary>
		/// Set to the interval that should elapse in between enquire_link packets.
		/// Setting this to anything other than 0 will enable the timer, setting 
		/// it to 0 will disable the timer. Timer is started after next connection.
		/// Also, EnquireLink packets are only sent when no other traffic went on between
		/// state interval.
		/// </summary>
		public TimeSpan EnquireLinkInterval { get; set; } = TimeSpan.FromSeconds(0);

		/// <summary>
		/// Sets intervals the system will wait before trying to reconnect/rebind after a total network failure (due to cable problems, etc).
		/// </summary>
		public TimeSpan[] ReconnectIntervals { get; set; } = new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60) };

		// FIXME: Add documentation to public (config) properties.

		public bool DisableCheckCertificateRevocation { get; set; }

		public SslProtocols SupportedSslProtocols { get; private set; }

		public bool ThrowWhenAddExistingSequence { get; private set; } = false;

		public int RequestQueueMemoryLimitMegabytes { get; private set; } = 32;

		// FIXME: Optimize this.. and verify if locking maybe needed..
		public States State => _state;

		public bool Started => _started;

		#endregion
		
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
		
		#region events
		/// <summary>
		/// Event called when the client receives a bind response.
		/// </summary>
		public event BindRespEventHandler OnBindResp;
		/// <summary>
		/// Event called when an error occurs.
		/// </summary>
		public event ErrorEventHandler OnError;
		/// <summary>
		/// Event called when the client is unbound.
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
		/// Event called when a generic_nack is received.
		/// </summary>
		public event GenericNackRespEventHandler OnGenericNackResp;
		/// <summary>
		/// Event called when an enquire_link is received.
		/// </summary>
		public event EnquireLinkEventHandler OnEnquireLink;
		/// <summary>
		/// Event called when an unbind is received.
		/// </summary>
		public event UnbindEventHandler OnUnbind;
		/// <summary>
		/// Event called when the client receives a request for a bind.
		/// </summary>
		public event BindEventHandler OnBind;
		/// <summary>
		/// Event called when the client receives a cancel_sm.
		/// </summary>
		public event CancelSmEventHandler OnCancelSm;
		/// <summary>
		/// Event called when the client receives a cancel_sm_resp.
		/// </summary>
		public event CancelSmRespEventHandler OnCancelSmResp;
		/// <summary>
		/// Event called when the client receives a query_sm_resp.
		/// </summary>
		public event QuerySmRespEventHandler OnQuerySmResp;
		/// <summary>
		/// Event called when the client receives a data_sm.
		/// </summary>
		public event DataSmEventHandler OnDataSm;
		/// <summary>
		/// Event called when the client receives a data_sm_resp.
		/// </summary>
		public event DataSmRespEventHandler OnDataSmResp;
		/// <summary>
		/// Event called when the client receives a deliver_sm.
		/// </summary>
		public event DeliverSmEventHandler OnDeliverSm;
		/// <summary>
		/// Event called when the client receives a deliver_sm_resp.
		/// </summary>
		public event DeliverSmRespEventHandler OnDeliverSmResp;
		/// <summary>
		/// Event called when the client receives a replace_sm.
		/// </summary>
		public event ReplaceSmEventHandler OnReplaceSm;
		/// <summary>
		/// Event called when the client receives a replace_sm_resp.
		/// </summary>
		public event ReplaceSmRespEventHandler OnReplaceSmResp;
		/// <summary>
		/// Event called when the client receives a submit_multi.
		/// </summary>
		public event SubmitMultiEventHandler OnSubmitMulti;
		/// <summary>
		/// Event called when the client receives a submit_multi_resp.
		/// </summary>
		public event SubmitMultiRespEventHandler OnSubmitMultiResp;
		/// <summary>
		/// Event called when the client' state changes.
		/// </summary>
		public event ClientStateChangedEventHandler OnClientStateChanged;

		#endregion events

		#region constructors
		
		static SMPPClient()
		{
			// FIXME: Adapt internal DotNetty logging to Common.Logging..
			// FIXME: Add a new InternalLogging property in order to enable / disable logging of DotNetty code.
			// We may want to use: https://github.com/hippasus/Common.Logging.MicrosoftLogging/tree/main/src/Common.Logging.MicrosoftLogging
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
		public SMPPClient(string host, ushort port, SslProtocols ssl = SslProtocols.None)
		{
			Host = host;
			Port = port;
			SupportedSslProtocols = ssl;

			_eventLoopGroup = new SingleThreadEventLoop();
			//_eventLoopGroup = new MultithreadEventLoopGroup();
		}
		#endregion constructors

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

		private void Setup(IChannelPipeline pipeline)
		{
			if (SupportedSslProtocols != SslProtocols.None)
			{
				ClientTlsSettings tlsSettings = new ClientTlsSettings(
					SupportedSslProtocols, !DisableCheckCertificateRevocation,
					new List<X509Certificate>(), Host);
				pipeline.AddLast("tls", new TlsHandler(tlsSettings));
			}

			pipeline
				.AddLast(new LoggingHandler())
				.AddLast("framing-dec",
					new LengthFieldBasedFrameDecoder(ByteOrder.BigEndian, Int32.MaxValue, 0, 4, -4, 0, false))
				.AddLast("pdu-codec", new PduCodec())
				.AddLast("enquire-link", new EnquireLinkHandler(this))
				.AddLast("channel-handler", new ChannelHandler(this))
				.AddLast("resilient-handler", new ResilientHandler(this))
				;
		}

		private Bootstrap CreateChannelFactory()
		{
			return new Bootstrap()
				.Group(_eventLoopGroup)
				.Channel<TcpSocketChannel>()
				.Option(ChannelOption.TcpNodelay, true)
				//.Option(ChannelOption.SoKeepalive, true)
				.Option(ChannelOption.SoLinger, 0)
				.Option(ChannelOption.SoRcvbuf, (int)CHANNEL_BUFFER_SIZE)
				.Option(ChannelOption.SoSndbuf, (int)CHANNEL_BUFFER_SIZE)
				.Option(ChannelOption.ConnectTimeout, ConnectTimeout)
				.Option(ChannelOption.SoTimeout, (int)SocketTimeout.TotalMilliseconds)
				.RemoteAddress(Host, Port)
				.Handler(new ActionChannelInitializer<ISocketChannel>(channel => Setup(channel.Pipeline)))
				.Validate();
		}

		private Bootstrap GetOrCreateChannelFactory()
		{
			// XXX: We need to delay creation of channel factory
			//		so user can set additional config properties
			//		not received via .ctor before calling Start or Connect.

			if (_channelFactory == null)
			{
				_channelFactory = CreateChannelFactory();
			}

			return _channelFactory;
		}

		private void SetChannel(IChannel channel)
		{
			lock (_lock)
			{
				Guard.Argument(channel).NotNull(nameof(channel));
				Guard.Operation(_channel == null, "A channel was already set while setting a new one?!");
				Guard.Operation(State is States.Inactive, $"Setting a channel on a client w/ state {State}?!");

				_channel = channel;
			}
		}

		private void ReleaseChannel()
		{
			lock (_lock)
			{
				Guard.Operation(_channel != null, "A channel was not present while trying to release it?!");
				_channel.Unsafe.CloseForcibly();
				_channel = null;
			}
		}

		private void SetNewState(States newState)
		{
			var oldState = _state;
			_state = newState;
			if (oldState != newState)
				OnClientStateChanged?.Invoke(this, new ClientStateChangedEventArgs(oldState, newState));
		}

		private SmppBind CreateBind()
		{
			return new SmppBind()
			{
				SystemId = SystemId,
				Password = Password,
				SystemType = SystemType,
				InterfaceVersion = Version,
				AddressTon = TonType,
				AddressNpi = NpiType,
				AddressRange = AddressRange,
				BindType = BindType,
			};
		}

		private void ConnectDetached()
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			_Log.DebugFormat(" ==> ConnectDetached begin..");

			if (!_started) //< TODO: Find a way to pass this as context attached to actual re-connection cycle..
			{
				_Log.DebugFormat("Ignoring ConnectDetached call when not started anymore..");
				return;
			}

			lock (_lock)
			{
				Guard.Operation(State is States.Inactive, $"Can't connect a client w/ state {State}, already connected?!");
				Guard.Operation(_channel == null, $"Trying to connect while a channel still exists?!");
				Guard.Operation(_connecting == null, $"Trying to connect while already connecting?!");

				_connecting = GetOrCreateChannelFactory()
					.ConnectAsync()
					.ContinueWith(x =>
					{
						_Log.DebugFormat(" ==> ConnectDetached finished..");
						_connecting = null;

						if (x.Exception != null)
						{
							try
							{
								this.OnError?.Invoke(this, new SmppExceptionEventArgs(x.Exception));
							}
							catch { }
						}
					}); //< XXX: Not awaited on purpose..
			}
		}

		#endregion private methods

		/// <summary>
		/// Starts SMPP client w/ background reconnecting / re-binding as needed.
		/// </summary>
		/// <returns></returns>
		public void Start()
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			try
			{
				_Log.DebugFormat("Starting client for {0}:{1}...", Host, Port);

				lock (_lock)
				{
					Guard.Operation(!Started, "Invalid call 'Start()' on a client already 'Started'?!");
					Guard.Operation(State is States.Inactive, $"Can't start a client w/ state {State}, already connected?!");

					_started = true; //< Enable auto connection handling..
					_eventLoopGroup.Execute(() => ConnectDetached());
				}
			}
			catch (Exception ex)
			{
				ex = (ex as AggregateException)?.Flatten() ?? ex;
				_Log.ErrorFormat("Error connecting to {0}:{1}.", ex, Host, Port);
				throw; //< XXX: At this point we _should_ throw, other wise caller wont notice client was not started.
			}
		}

		public void Stop()
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			_Log.DebugFormat("Stopping client for {0}:{1}...", Host, Port);

			lock (_lock)
			{
				Guard.Operation(Started, "Invalid call to 'Stop()' on a client not 'Started'?!");

				_started = false; //< Disable auto connection handling..

				if (State == States.Bound)
				{
					Unbind(); //< FIXME: Do we really need/want to be so polite?
				}

				if (_channel != null)
				{
					_channel.CloseAsync().GetAwaiter().GetResult();
				}
			}
		}

		public void Connect()
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			_Log.DebugFormat("Connecting client for {0}:{1}...", Host, Port);

			lock (_lock) //< We need to lock due to manipulating _channel..
			{
				Guard.Operation(_started == false, "Calling Connect/Disconnect with auto connection handling enabled not supported.");
				Guard.Operation(State is States.Inactive, $"Can't connect a client w/ state {State}, already connected?!");
				Guard.Operation(_channel == null, $"Trying to connect while a channel still exists?!");

				GetOrCreateChannelFactory().ConnectAsync().GetAwaiter().GetResult();

				Guard.Operation(_channel != null, $"No channel establish after connecting?!");
			}
		}

		public void Disconnect()
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			_Log.DebugFormat("Disconnecting client for {0}:{1}...", Host, Port);

			lock (_lock)
			{
				Guard.Operation(_started == false, "Calling Connect/Disconnect with auto connection handling enabled not supported.");
				Guard.Operation(State >= States.Connected, $"Can't disconnect an a client w/ state {State}, not connected yet?");
				Guard.Operation(_channel != null, $"Trying to connect while no channel present?!");

				using (var disconnectCancellator = new CancellationTokenSource(DisconnectTimeout))
				using (var linkedCancellators = CancellationTokenSource.CreateLinkedTokenSource(_cancellator.Token, disconnectCancellator.Token))
				{
					_channel.DisconnectAsync()
						.WithCancellation(linkedCancellators.Token)
						.GetAwaiter().GetResult();
				}

				Guard.Operation(_channel == null, $"Channel still present after disconnecting?!");
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
			_cancellator.Token.ThrowIfCancellationRequested();

			GuardEx.Against(_channel == null, "Can't bind non-connected client, call ConnectAsync() first.");
			GuardEx.Against(State == States.Bound, "Already bound to remote party, unbind session first.");
			GuardEx.Against(State != States.Connected, "Can't bind non-connected session, call ConnectAsync() first.");

			_Log.InfoFormat("Binding to {0}:{1}..", Host, Port);
			
			SendAndWait(CreateBind());
			
			_Log.InfoFormat("Bound to {0}:{1}.", Host, Port);
		}

		/// <summary>
		/// Unbinds the SMPPClient from the SMSC when it receives the unbind response from the SMSC.
		/// This will also stop the timer that sends out the enquire_link packets if it has been enabled.
		/// You need to explicitly call this to unbind.; it will not be done for you.
		/// </summary>
		public void Unbind()
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			GuardEx.Against(_channel == null, "Can't unbind non-connected client.");
			Guard.Operation(State == States.Bound, $"Can't unbind a session w/ state {State}, try binding first.");

			_Log.InfoFormat("Unbinding from {0}:{1}..", Host, Port);

			SendAndWait(new SmppUnbind());
			
			_Log.InfoFormat("Unbound from {0}:{1}.", Host, Port);
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
				
				GuardEx.Against(_channel == null, "Channel has not been initialized?!");
				Guard.Operation(_channel?.Active == true, "Channel is not active?!.");
				GuardEx.Against(State < States.Connected, "Session is not connected.");
				GuardEx.Against(packet is SmppBind && State != States.Connected, "Session is not connected.");
				GuardEx.Against(packet is not SmppBind && State != States.Bound, "Session not bound to remote party.");

				var timeout = (int)RequestTimeout.TotalMilliseconds;
				_channel!.WriteAndFlushAsync(packet).Wait(timeout, _cancellator.Token);
				return packet.SequenceNumber;
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

			var promise = request.EnableResponseTracking();
			
			SendPdu(request);
			
			if (promise.Wait((int)ResponseTimeout.TotalMilliseconds, _cancellator.Token))
			{
				var response = promise.Result;

				if (response is SmppGenericNackResp nack)
				{
					var msg = $"SmppRequest was rejected by remote party. (error: {nack.CommandStatus})";
					throw new SmppRequestException(msg, request, nack);
				}

				return response.CommandStatus == CommandStatus.ESME_ROK ? response
					: throw new SmppRequestException($"{request.GetType().Name} failed.", request, response);
			}
			
			throw new SmppTimeoutException("Timeout while waiting for a response from remote side.");
		}

		public IEnumerable<SmppResponse> SendAndWait(IEnumerable<SmppRequest> requests)
		{
			var tasks = new Task<SmppResponse>[requests.Count()];

			for (int i = 0; i < tasks.Length; i++)
			{
				var request = requests.ElementAt(i);
				tasks[i] = request.EnableResponseTracking();
				SendPdu(request);
			}

			if (!Task.WaitAll(tasks, (int)ResponseTimeout.TotalMilliseconds, _cancellator.Token))
			{
				throw new SmppTimeoutException(string.Format(
					"Timeout while waiting for a responses from remote side. (Missing: {0}/{1})",
					tasks.Select(x => !x.IsCompleted).Count(), tasks.Length
				));
			}

			var responses = tasks.Select(x => x.Result).ToArray();
			if (responses.Any(x => x is SmppGenericNackResp))
			{
				var nack = responses.First(x => x is SmppGenericNackResp);
				var msg = $"At least one SmppRequest was rejected by remote party. (error: {nack.CommandStatus})";
				throw new SmppRequestsException(msg, requests, responses);
			}
			else if (responses.Any(x => x.CommandStatus != CommandStatus.ESME_ROK))
			{
				throw new SmppRequestsException("SmppRequest failed.", requests, responses);
			}

			return responses;
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
			
			return responses.OfType<SmppSubmitSmResp>()
				.Select(x => new
				{
					Request = requests.Single(r => r.SequenceNumber == x.SequenceNumber),
					Response = x
				})
				.ToDictionary(x => (SmppSubmitSm)x.Request, x => x.Response);
		}
		
		/// <summary>
		/// Disposes of this component.  Called by the framework; do not call it 
		/// directly.
		/// </summary>
		public void Dispose()
		{
			if (_eventLoopGroup.IsShuttingDown || _eventLoopGroup.IsShutdown)
			{
				_Log.WarnFormat("Ignoring repeated call to Dispose() on client for {0}:{1}..", Host, Port);
				return;
			}

			_Log.DebugFormat("Disposing session for {0}:{1}", Host, Port);

			_cancellator.Cancel();
			//Helper.ShallowExceptions(() => { _channel?.CloseAsync().Wait(500); });
			Helper.ShallowExceptions(() => { _channel?.DisconnectAsync().Wait(DisconnectTimeout); });
			Helper.ShallowExceptions(() => { _eventLoopGroup?.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500)).Wait(); });

			_channel = null;

			_Log.DebugFormat("Disposed session for {0}:{1}", Host, Port);
		}
	}
}
