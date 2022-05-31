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
using System.Threading.Tasks;
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
		
		private CancellationTokenSource _cancellator = new CancellationTokenSource();

		private readonly object _lock = new object();
		private Bootstrap _channelFactory;
		private IEventLoopGroup _eventLoopGroup;
		private IChannel _channel;
		private volatile States _state;
		private volatile uint _EnquireLinkInterval;
		private Random _random = new Random();
		private uint _channelBufferSize = 10240;
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
		/// Gets or sets the request timeout (in miliseconds)
		/// </summary>
		/// <value>The response timeout.</value>
		public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
		
		/// <summary>
		/// Gets or sets the response timeout (in miliseconds)
		/// </summary>
		/// <value>The response timeout.</value>
		public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(10);
		
		/// <summary>
		/// Set to the interval that should elapse in between enquire_link packets.
		/// Setting this to anything other than 0 will enable the timer, setting 
		/// it to 0 will disable the timer.  Note that the timer is only started/stopped 
		/// during a bind/unbind.
		/// Also, EnquireLink packets are only sent when no other traffic went on between
		/// state interval.
		/// </summary>
		public TimeSpan EnquireLinkInterval
		{
			get => TimeSpan.FromMilliseconds(_EnquireLinkInterval);
			set => _EnquireLinkInterval = (uint)value.TotalMilliseconds;
		}
		
		/// <summary>
		/// Sets the number of seconds that the system will wait before trying to rebind 
		/// after a total network failure(due to cable problems, etc).  Negative values are 
		/// ignored, and 0 disables Re-Binding.
		/// </summary>
		public TimeSpan ReBindInterval { get; set;  }

		public SslProtocols SupportedSslProtocols { get; set; }

		// FIXME: Optimize this.. and verify if locking maybe needed..
		public States State => _state;

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

			// Initialize timers..
			//_ReBindTimer = new System.Timers.Timer() { Enabled = false };
			//_ReBindTimer.Elapsed += new ElapsedEventHandler(ReBindTimerElapsed);

			_eventLoopGroup = new MultithreadEventLoopGroup();
			_channelFactory = new Bootstrap()
				.Group(_eventLoopGroup)
				.Channel<TcpSocketChannel>()
				.Option(ChannelOption.TcpNodelay, true)
				.Option(ChannelOption.SoLinger, 0)
				.Option(ChannelOption.SoRcvbuf, (int)_channelBufferSize)
				.Option(ChannelOption.SoSndbuf, (int)_channelBufferSize)
				//.RemoteAddress(Host, Port)
				.Handler(new ActionChannelInitializer<ISocketChannel>(channel => Setup(channel.Pipeline, this)));
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
			
			//_ReBindTimer.Stop(); // (temporarilly) disable re-binding timer.
			
			//	finally
			{
				// Re-enable rebinding timer..
			//	if (_ReBindInterval > 0)
				{
			//		_ReBindTimer.Interval = _ReBindInterval * 1000;
			//		_ReBindTimer.Start();
				}
			}
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}
		
		public void Connect()
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			Guard.Operation(State is States.Inactive, $"Can't connect a client w/ state {State}, already connected?");

			_Log.DebugFormat("Connecting to {0}:{1}.", Host, Port);
			
			lock (_lock)
			{
				// FIXME: Pass timeout & cancellator..
				_channel = _channelFactory.ConnectAsync(Host, Port)
					.WithCancellation(_cancellator.Token)
					.GetAwaiter().GetResult();
			}

			_Log.DebugFormat("Connected to {0}:{1}.", Host, Port);

		}

		public void Disconnect()
		{
			_cancellator.Token.ThrowIfCancellationRequested();

			Guard.Operation(State >= States.Connected, $"Can't disconnect an a client w/ state {State}, not connected yet?");
			
			_Log.DebugFormat("Disconnecting from {0}:{1}.", Host, Port);

			lock (_lock)
			{
				// FIXME: Pass timeout & cancellator..
				_channel.DisconnectAsync()
					.WithCancellation(_cancellator.Token)
					.GetAwaiter().GetResult();
				_channel = null;
			}

			_Log.DebugFormat("Disconnected from {0}:{1}.", Host, Port);

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
			GuardEx.Against(_channel == null, "Can't bind non-connected client, call Connect() first.");
			GuardEx.Against(State == States.Bound, "Already bound to remote party, unbind session first.");
			GuardEx.Against(State != States.Connected, "Can't bind non-connected session, call Connect() first.");

			_Log.InfoFormat("Binding to {0}:{1}..", Host, Port);
			
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
			
			var response = SendAndWait(new SmppUnbind());
			
			if (response.CommandStatus != 0)
				throw new SmppRequestException("Unbind request failed.", response.CommandStatus);
			
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
				GuardEx.Against(!(packet is SmppBind) && State != States.Bound, "Session not bound to remote party.");

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
					: throw new SmppRequestException("SmppRequest failed.", request, response);
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
		
		private static void Setup(IChannelPipeline pipeline, SMPPClient client)
		{
			if (client.SupportedSslProtocols != SslProtocols.None)
			{
				//pipeline.AddLast("tls", new TlsHandler(stream => new SslStream()))
			}

			pipeline
				.AddLast(new LoggingHandler())
				//.AddLast("framing-enc", new LengthFieldPrepender(4, true))
				.AddLast("framing-dec",
					new LengthFieldBasedFrameDecoder(ByteOrder.BigEndian, Int32.MaxValue, 0, 4, -4, 0, false))
				.AddLast("pdu-codec", new PduCodec())
				.AddLast("enquire-link", new EnquireLinkHandler(client.EnquireLinkInterval))
				.AddLast("channel-handler", new ChannelHandler(client));
		}

		private void SetNewState(States newState)
		{
			var oldState = _state;
			_state = newState;
			OnClientStateChanged?.Invoke(this, new ClientStateChangedEventArgs(oldState, newState));
		}
		
		#if false
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

		#endif
		#endregion private methods

		/// <summary>
		/// Disposes of this component.  Called by the framework; do not call it 
		/// directly.
		/// </summary>
		public void Dispose()
		{
			_Log.DebugFormat("Disposing session for {0}:{1}", Host, Port);
			
			//Helper.ShallowExceptions(() => Unbind());
			_cancellator.Cancel();
			Helper.ShallowExceptions(() => { _channel?.DisconnectAsync().Wait(500); });
			Helper.ShallowExceptions(() => { _eventLoopGroup.ShutdownGracefullyAsync(TimeSpan.Zero, TimeSpan.FromMilliseconds(500)); });
		}
	}
}
