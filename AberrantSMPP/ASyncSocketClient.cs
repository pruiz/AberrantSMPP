/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 * Copyright (C) 2010, 2011 Pablo Ruiz Garc�a <pruiz@crt0.net>
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
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

//#define HARDCORE_LOGGING

namespace AberrantSMPP
{
	/// <summary>
	/// Socket class for asynchronous connection.
	/// </summary>
	internal class AsyncSocketClient : IDisposable
	{
		private static readonly global::Common.Logging.ILog _Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		#region delegates

		/// <summary>
		/// Called when a message is received.
		/// </summary>
		/// <param name="asyncSocketClient">
		/// The AsyncSocketClient to receive messages from.
		/// </param>
		public delegate void MessageHandler(AsyncSocketClient asyncSocketClient);
	
		/// <summary>
		/// Called when a connection is closed.
		/// </summary>
		/// <param name="asyncSocketClient">
		/// The AsyncSocketClient to receive messages from.
		/// </param>
		public delegate void SocketClosingHandler(
			AsyncSocketClient asyncSocketClient);
	
		/// <summary>
		/// Called when a socket error occurs.
		/// </summary>
		/// <param name="asyncSocketClient">
		/// The AsyncSocketClient to receive messages from.
		/// </param>
		/// <param name="exception">
		/// The exception that generated the error.
		/// </param>
		public delegate void ErrorHandler(
			AsyncSocketClient asyncSocketClient, Exception exception);
	
		#endregion delegates
		
		//private const int BUFFER_SIZE = 1048576; // One MB
		private const int BUFFER_SIZE = 262144;

		#region private fields
		private ReaderWriterLockSlim _socketLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		private readonly Queue<byte[]> _SendQueue = new Queue<byte[]>();
		private readonly byte[] _Buffer;
		private TcpClient _TcpClient;
		private AsyncCallback _CallbackReadMethod;
		private AsyncCallback _CallbackWriteMethod;
		private MessageHandler _MessageHandler;
		private SocketClosingHandler _SocketCloseHandler;
		private ErrorHandler _ErrorHandler;
		private bool _IsDisposed;
		private string _ServerAddress;
		private UInt16 _ServerPort;
		private object _StateObject;
#if HARDCORE_LOGGING
		private bool __SendPending;
		private bool _SendPending
		{
			get
			{
				_Log.DebugFormat("SendPending ==(GET - {1})==>> {0}", __SendPending, _ThreadId);
				return __SendPending;
			}
			set
			{
				_Log.DebugFormat("SendPending ==(SET - {1})==>> {0}", __SendPending, _ThreadId);
				__SendPending = value;
			}
		}
#else
		private bool _SendPending;
#endif
		#endregion private fields

		#region properties
		private string _ThreadId
		{
			get
			{
				return string.Format("{0} ({1})",
					System.Threading.Thread.CurrentThread.Name,
					System.Threading.Thread.CurrentThread.ManagedThreadId
				);
			}
		}
		/// <summary>
		/// The server address to connect to.
		/// </summary>
		public string ServerAddress
		{
			get
			{
				return _ServerAddress;
			}
		}
		/// <summary>
		/// The server port to connect to.
		/// </summary>
		public UInt16 ServerPort
		{
			get
			{
				return _ServerPort;
			}
		}
		/// <summary>
		/// A user set state object to associate some state with a connection.
		/// </summary>
		public object StateObject
		{
			get
			{
				return _StateObject;
			}
			set
			{
				_StateObject = value;
			}
		}
		/// <summary>
		/// Buffer to hold data coming in from the socket.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				return _Buffer;
			}
		}
		/// <summary>
		/// Gets a value indicating whether this <see cref="AsyncSocketClient"/> is connected.
		/// </summary>
		/// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
		public bool Connected
		{
			get {
				using (new ReadOnlyLock(_socketLock))
				{
					return _TcpClient != null && _TcpClient.Connected && _TcpClient.Client.Connected;
				}
			}
		}
		#endregion properties

		/// <summary>
		/// Constructs an AsyncSocketClient.
		/// </summary>
		/// <param name="bufferSize">The size of the receive buffer.</param>
		/// <param name="stateObject">The object to use for sending state
		/// information.</param>
		/// <param name="msgHandler">The user-defined message handling method.
		/// </param>
		/// <param name="closingHandler">The user-defined socket closing
		/// handling method.</param>
		/// <param name="errHandler">The user defined error handling method.
		/// </param>
		public AsyncSocketClient(Int32 bufferSize, object stateObject,
		                         MessageHandler msgHandler, SocketClosingHandler closingHandler,
		                         ErrorHandler errHandler)
		{
			_Log.DebugFormat("Initializing new instance ({0}).", this.GetHashCode());

			_Buffer = new byte[BUFFER_SIZE];
			_StateObject = stateObject;

			//set handlers
			_MessageHandler = msgHandler;
			_SocketCloseHandler = closingHandler;
			_ErrorHandler = errHandler;

			//set the asynchronous method handlers
			_CallbackReadMethod = new AsyncCallback(ReceiveComplete);
			_CallbackWriteMethod = new AsyncCallback(SendComplete);

			//haven't been disposed yet
			_IsDisposed = false;
		}

		/// <summary>
		/// Finalizer method.  If Dispose() is called correctly, there is nothing
		/// for this to do.
		/// </summary>
		~AsyncSocketClient()
		{
			if (!_IsDisposed)
			{
				Dispose();
			}
		}

		#region public methods
		/// <summary>
		/// Sets the disposed flag to true and disconnects the socket.
		/// </summary>
		public void Dispose()
		{
			using (new WriteLock(_socketLock))
			{
				try
				{
					_Log.DebugFormat("Disposing instance ({0}).", this.GetHashCode());
					_IsDisposed = true;
					Disconnect();
				}
				catch (Exception ex)
				{
					_Log.Warn("Exception thrown while disposing.", ex);
				}
			}
		}

		/// <summary>
		/// Connects the socket to the given IP address and port.
		/// This also calls Receive().
		/// </summary>
		/// <param name="address">The IP address of the server.</param>
		/// <param name="port">The port to connect to.</param>
		public void Connect(String address, UInt16 port)
		{
			using (new WriteLock(_socketLock))
			{
				//do we already have an open connection?
				if (_TcpClient != null)
					throw new InvalidOperationException("Already connected to remote host.");

				_Log.DebugFormat("Connecting instance ({0}) to {1}:{2}.", this.GetHashCode(), address, port);

				_ServerAddress = address;
				_ServerPort = port;

				//attempt to establish the connection
				_TcpClient = new TcpClient(_ServerAddress, _ServerPort);

				//set some socket options
				_TcpClient.ReceiveBufferSize = BUFFER_SIZE;
				_TcpClient.SendBufferSize = BUFFER_SIZE;
				_TcpClient.NoDelay = true;
				//if the connection is dropped, drop all associated data
				_TcpClient.LingerState = new LingerOption(false, 0);
			}

			//start receiving messages
			Receive();
		}

		///<summary>
		/// Disconnects from the server.
		/// </summary>
		public void Disconnect()
		{
			using (new WriteLock(_socketLock))
			{

				_Log.DebugFormat("Disconnecting instance ({0}).", this.GetHashCode());

				//close down the connection, making sure it exists first
				if (_TcpClient != null)
				{
					Helper.ShallowExceptions(() => _TcpClient.Close());
				}

				Helper.ShallowExceptions(() => _SendQueue.Clear());

				//prep for garbage collection-we may want to use this instance again
				_TcpClient = null;
			}
		}

		///<summary>
		/// Asynchronously sends data across the socket.
		/// </summary>
		/// <param name="buffer">
		/// The buffer of data to send.
		/// </param>
		public void Send(byte[] buffer)
		{
			using (new ReadOnlyLock(_socketLock))
			{
#if HARDCORE_LOGGING
				_Log.DebugFormat("Instance {0} - {1} => Sending..", this.GetHashCode(), _ThreadId);
#endif

				if (!this.Connected)
					throw new IOException("Socket is closed, cannot Send().");

				lock (_SendQueue)
				{
					if (_SendPending)
					{
						_Log.DebugFormat("Instance {0} - {1} => Queuing data for transmission..", this.GetHashCode(), _ThreadId);
						_SendQueue.Enqueue(buffer);
					}
					else
					{
#if HARDCORE_LOGGING
						_Log.DebugFormat("Instance {0} - {1} => Sending actual data..", this.GetHashCode(), _ThreadId);
#endif

						//send the data; don't worry about receiving any state information back;
						_SendPending = true; 
						_TcpClient.GetStream()
							.BeginWrite(buffer, 0, buffer.Length, _CallbackWriteMethod, null);
#if HARDCORE_LOGGING
						_Log.DebugFormat("Instance {0} - {1} => Data sent..", this.GetHashCode(), _ThreadId);
#endif
					}
				}
			}
		}

		/// <summary>
		/// Asynchronously receives data from the socket.
		/// </summary>
		// XXX: Method is now private as it's called internally only. (pruiz)
		private void Receive()
		{
			using (new ReadOnlyLock(_socketLock))
			{
				if (!this.Connected)
					throw new IOException("Socket is closed, cannot Receive().");

				Array.Clear(_Buffer, 0, _Buffer.Length); // Clear contents..

				_TcpClient.GetStream()
					.BeginRead(_Buffer, 0, _Buffer.Length, _CallbackReadMethod, null);
			}
		}
		#endregion public methods

		#region private methods
		/// <summary>
		/// Callback method called by the NetworkStream's thread when a message
		/// is sent.
		/// </summary>
		/// <param name="state">The state object holding information about
		/// the connection.</param>
		private void SendComplete(IAsyncResult state)
		{
#if HARDCORE_LOGGING
			_Log.DebugFormat("Instance {0} - {1} => Send completed.", this.GetHashCode(), _ThreadId);
#endif

			try
			{
				using (new ReadOnlyLock(_socketLock))
				{
#if HARDCORE_LOGGING
					_Log.DebugFormat("Instance {0} - {1} => Finishing sent operation..", this.GetHashCode(), _ThreadId);
#endif

					if (!_SendPending) 
						_Log.ErrorFormat("Instance {0} - {1} => SendComplete called while SendPending=False?!?!?", this.GetHashCode(), _ThreadId);

					_SendPending = false;

					if (_TcpClient != null)
						_TcpClient.GetStream().EndWrite(state);
				}
			}
			catch (Exception ex)
			{
				_Log.Warn(string.Format("Instance {0} - {1} => Async send failed.", this.GetHashCode(), _ThreadId), ex);
			}

			// If there are more packets to send..

			lock (_SendQueue)
			{
#if HARDCORE_LOGGING
				_Log.DebugFormat("Instance {0} - {1} => Processing send queue.", this.GetHashCode(), _ThreadId);
#endif

				if (_SendQueue.Count == 0)
				{
#if HARDCORE_LOGGING
					_Log.DebugFormat("Instance {0} - {1} => No packets on queue..", this.GetHashCode(), _ThreadId);
#endif
					// Reduce queue internal space..
					_SendQueue.TrimExcess();
					return;
				}

#if HARDCORE_LOGGING
				_Log.DebugFormat("Instance {0} - {1} => Sending queued packet.", this.GetHashCode(), _ThreadId);
#endif

				// Send another packet..
				using (new ReadOnlyLock(_socketLock))
				{
					_SendPending = true;
					try
					{
						var packet = _SendQueue.Dequeue();
						_TcpClient.GetStream().BeginWrite(packet, 0, packet.Length, _CallbackWriteMethod, null);
					}
					catch (Exception ex)
					{
						_Log.Warn(string.Format("Instance {0} - {1} => Async send failed.", this.GetHashCode(), _ThreadId), ex);
						_SendPending = false;
					}
				}
			}
		}

		/// <summary>
		/// Callback method called by the NetworkStream's thread when a message
		/// arrives.
		/// </summary>
		/// <param name="state">The state object holding information about
		/// the connection.</param>
		private void ReceiveComplete(IAsyncResult state)
		{
			try
			{
				int bytesReceived = 0;

				using (new ReadOnlyLock(_socketLock))
				{
					if (_TcpClient != null)
						bytesReceived = _TcpClient.GetStream().EndRead(state);
				}

				//if there are bytes to process, do so.  Otherwise, the
				//connection has been lost, so clean it up
				if (bytesReceived > 0)
				{
					try
					{
						//send the incoming message to the message handler
						_MessageHandler(this);
					}
					catch (Exception ex)
					{
						_Log.Error(string.Format("Instance {0} - {1} => Receive message handler failed.", this.GetHashCode(), _ThreadId), ex);
					}
				}

				// XXX: If readBytesCount == 0 --> Connection has been closed.

				// XXX: Access _TcpClient.Client.Available to ensure socket is still working..

				//start listening again
				Receive();
			}
			catch (Exception ex)
			{
				_Log.Warn("Receive failed.", ex);

				//the connection has been dropped so call the CloseHandler
				try
				{
					_SocketCloseHandler(this);
				}
				finally
				{
					_Log.WarnFormat("Instance {0} - {1} => Connection terminated, disposing..", this.GetHashCode(), _ThreadId);
					Dispose();
				}
			}
		}
		#endregion private methods
	}
}
