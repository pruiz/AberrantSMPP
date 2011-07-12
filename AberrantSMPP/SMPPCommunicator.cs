/* RoaminSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
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
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Utility;
using AberrantSMPP.Packet.Response;
using AberrantSMPP.Packet;
using AberrantSMPP.EventObjects;
using System.Timers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AberrantSMPP 
{
	/// <summary>
	/// Wrapper class to provide asynchronous I/O for the RoaminSMPP library.  Note that most 
	/// SMPP events have default handlers.  If the events are overridden by the caller by adding 
	/// event handlers, it is the caller's responsibility to ensure that the proper response is 
	/// sent.  For example: there is a default deliver_sm_resp implemented.  If you "listen" to 
	/// the deliver_sm event, it is your responsibility to then send the deliver_sm_resp packet.
	/// </summary>
	public class SMPPCommunicator : Component
	{
		private AsyncSocketClient asClient;
		private Int16 _Port;
		private SmppBind.BindingType _BindType;
		private string _Host;
		private Pdu.NpiType _NpiType;
		private Pdu.TonType _TonType; 
		private SmppBind.SmppVersionType _Version;
		private string _AddressRange;
		private string _Password;
		private string _SystemId;
		private string _SystemType;
		private System.Timers.Timer timer;
		private int _EnquireLinkInterval;
		private int _SleepTimeAfterSocketFailure;
		private bool _SentUnbindPacket = true;  //default to true since we start out unbound
		private string username;
		
		/// <summary>
		/// Required designer variable.
		/// </summary>
		protected Container components = null;

		#region properties
		
		/// <summary>
		/// The username to use for software validation.
		/// </summary>
		public string Username
		{
			set
			{
				username = value;
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
		/// The port on the SMSC to connect to.
		/// </summary>
		public Int16 Port
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
		/// ignored.
		/// </summary>
		public int SleepTimeAfterSocketFailure
		{
			get 
			{
				return _SleepTimeAfterSocketFailure;
			}

			set
			{
				if(value >= 0)
					_SleepTimeAfterSocketFailure = value;
			}
		}
		#endregion properties
		
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
			container.Add(this);
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
		{
			InitCommunicator();
			
			InitializeComponent();
		}

		#endregion constructors

		
		private void delTest() {
		
		}
		/// <summary>
		/// Sends a user-specified Pdu(see the RoaminSMPP base library for
		/// Pdu types).  This allows complete flexibility for sending Pdus.
		/// </summary>
		/// <param name="packet">The Pdu to send.</param>
		public void SendPdu(Pdu packet)
		{
			bool sendFailed = true;
			
			while(sendFailed)
			{
				try
				{
					packet.ToMsbHexEncoding();
					asClient.Send(packet.PacketBytes);
					sendFailed = false;
				}
				catch(Exception exc)
				{
					if(OnError != null)
					{
						OnError(this, new CommonErrorEventArgs(exc));
					}

					//try to stay alive
					if((exc.Message.ToLower().IndexOf("socket is closed")>= 0 || 
						exc.Message.ToLower().IndexOf("unable to write data to the transport connection")>= 0))
					{
						System.Threading.Thread.Sleep(SleepTimeAfterSocketFailure * 1000);
						Bind();
					}
					else	
					{
						//don't know what happened, but kick out
						sendFailed = false;
					}
				}
			}
		}

		/// <summary>
		/// Connects and binds the SMPPCommunicator to the SMSC, using the
		/// values that have been set in the constructor and through the
		/// properties.  This will also start the timer that sends enquire_link packets 
		/// at regular intervals, if it has been enabled.
		/// </summary>
		public void Bind()
		{
			try
			{
				if(asClient != null)
					asClient.Disconnect();
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

				asClient.Connect(Host, Port);

				SmppBind request = new SmppBind();
				request.SystemId = SystemId;
				request.Password = Password;
				request.SystemType = SystemType;
				request.InterfaceVersion = Version;
				request.AddressTon = TonType;
				request.AddressNpi = NpiType;
				request.AddressRange = AddressRange;
				request.BindType = BindType;

				SendPdu(request);
				_SentUnbindPacket = false;

				if(_EnquireLinkInterval > 0)
				{
					if(timer == null)
					{
						timer = new System.Timers.Timer();
						timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
					}

					if(timer != null)		//reset the old timer
					{
						timer.Stop();

						timer.Interval = EnquireLinkInterval * 1000;
						timer.Start();
					}
				}
			} 
			catch(Exception exc)
			{
				if(OnError != null)
				{
					OnError(this, new CommonErrorEventArgs(exc));
				}
			}
		}

		/// <summary>
		/// Unbinds the SMPPCommunicator from the SMSC then disconnects the socket
		/// when it receives the unbind response from the SMSC.  This will also stop the 
		/// timer that sends out the enquire_link packets if it has been enabled.  You need to 
		/// explicitly call this to unbind.; it will not be done for you.
		/// </summary>
		public void Unbind()
		{
			if(timer != null)
				timer.Stop();
			
			if(!_SentUnbindPacket)
			{
				SmppUnbind request = new SmppUnbind();
				SendPdu(request);
				_SentUnbindPacket = true;
			}
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
				Queue responseQueue = new PduFactory().GetPduQueue(client.Buffer);
				ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessPduQueue), responseQueue);
			} 
			catch(Exception exception)
			{
				if(OnError != null)
				{
					CommonErrorEventArgs e = new CommonErrorEventArgs(exception);
					OnError(this, e);
				}
			}
		}

		/// <summary>
		/// Callback method to handle socket closing.
		/// </summary>
		/// <param name="client">The client to receive messages from.</param>
		internal void ClientCloseHandler(AsyncSocketClient client)
		{
			//fire off a closing event
			if(OnClose != null)
			{
				System.EventArgs e = new System.EventArgs();
				OnClose(this, e);
			}
		}

		/// <summary>
		/// Callback method to handle errors.
		/// </summary>
		/// <param name="client">The client to receive messages from.</param>
		/// <param name="exception">The generated exception.</param>
		internal void ClientErrorHandler(AsyncSocketClient client,
			Exception exception)
		{
			//fire off an error handler
			if(OnError != null)
			{
				CommonErrorEventArgs e = new CommonErrorEventArgs(exception);
				OnError(this, e);
			}
		}
		#endregion internal methods

		#region private methods
		
				/// <summary>
		/// Goes through the packets in the queue and fires events for them.  Called by the
		/// threads in the ThreadPool.
		/// </summary>
		/// <param name="queueStateObj">The queue of byte packets.</param>
		private void ProcessPduQueue(object queueStateObj)
		{
			Queue responseQueue = queueStateObj as Queue;

			foreach(Pdu response in responseQueue)
			{
				//based on each Pdu, fire off an event
				if(response != null)
					FireEvents(response);
			}
		}
		
		/// <summary>
		/// Sends out an enquire_link packet.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="ea"></param>
		private void TimerElapsed(object sender, ElapsedEventArgs ea)
		{
			SendPdu(new SmppEnquireLink());
		}

		/// <summary>
		/// Fires an event off based on what Pdu is sent in.
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
			SleepTimeAfterSocketFailure = 10;
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
			
			try
			{
				if(!_SentUnbindPacket)
					Unbind();
			}
			catch
			{
				//drop it on the floor
			}
			base.Dispose(disposing);
		}
	}
}
