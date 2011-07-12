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

namespace AberrantSMPP
{
	/// <summary>
	/// Socket class for asynchronous connection.
	/// </summary>
	internal class AsyncSocketClient : IDisposable
	{
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
		//one MB
		private const int BUFFER_SIZE = 1048576;

		#region private fields

		private NetworkStream _NetworkStream;
		private TcpClient _TcpClient;
		private AsyncCallback _CallbackReadMethod;
		private AsyncCallback _CallbackWriteMethod;
		private MessageHandler _MessageHandler;
		private SocketClosingHandler _SocketCloseHandler;
		private ErrorHandler _ErrorHandler;
		private bool _IsDisposed;
		private string _ServerIPAddress;
		private Int16 _ServerPort;
		private object _StateObject;
		private byte[] _Buffer;
		//private int clientBufferSize;

		#endregion private fields

		#region properties

		/// <summary>
		/// The server IP address to connect to.
		/// </summary>
		public string ServerIPAddress
		{
			get
			{
				return _ServerIPAddress;
			}
		}

		/// <summary>
		/// The server port to connect to.
		/// </summary>
		public Int16 ServerPort
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
			//allocate buffer
//			clientBufferSize = bufferSize;
//			_Buffer = new byte[clientBufferSize];

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
			try
			{
				_IsDisposed = true;
				Disconnect();
			}
			catch
			{}
		}

		/// <summary>
		/// Connects the socket to the given IP address and port.
		/// This also calls Receive().
		/// </summary>
		/// <param name="IPAddress">The IP address of the server.</param>
		/// <param name="port">The port to connect to.</param>
		public void Connect(String IPAddress, Int16 port)
		{
			try
			{
				//do we already have an open connection?
				if (_NetworkStream == null)
				{
					_ServerIPAddress = IPAddress;
					_ServerPort = port;

					//attempt to establish the connection
					_TcpClient = new TcpClient(_ServerIPAddress, _ServerPort);
					_NetworkStream = _TcpClient.GetStream();

					//set some socket options
					_TcpClient.ReceiveBufferSize = BUFFER_SIZE;
					_TcpClient.SendBufferSize = BUFFER_SIZE;
					_TcpClient.NoDelay = true;
					//if the connection is dropped, drop all associated data
					_TcpClient.LingerState = new LingerOption(false, 0);

					//start receiving messages
					Receive();
				}
			}
			catch (SocketException exc)
			{
				//the connect failed..pass the word on
				throw new Exception(exc.Message, exc.InnerException);
			}
		}

		///<summary>
		/// Disconnects from the server.
		/// </summary>
		public void Disconnect()
		{
			//close down the connection, making sure it exists first
			if (_NetworkStream != null)
			{
				_NetworkStream.Close();
			}
			if (_TcpClient != null)
			{
				_TcpClient.Close();
			}

			//prep for garbage collection-we may want to use this instance again
			_NetworkStream = null;
			_TcpClient = null;

		}

		///<summary>
		/// Asynchronously sends data across the socket.
		/// </summary>
		/// <param name="buffer">
		/// The buffer of data to send.
		/// </param>
		public void Send(byte[] buffer)
		{
			//send the data; don't worry about receiving any state information
			//back;
			try
			{
				if (_NetworkStream != null && _NetworkStream.CanWrite)
				{
					_NetworkStream.BeginWrite(
						buffer, 0, buffer.Length, _CallbackWriteMethod, null);
				}
				else
				{
					throw new Exception("Socket is closed, cannot Send().");
				}
			}
			catch
			{
				throw;
			}
		}

		/// <summary>
		/// Asynchronously receives data from the socket.
		/// </summary>
		public void Receive()
		{
			try
			{
				if (_NetworkStream != null && _NetworkStream.CanRead)
				{
					//_Buffer = new byte[clientBufferSize];
					_Buffer = new byte[BUFFER_SIZE];
					
					_NetworkStream.BeginRead(
						_Buffer, 0, _Buffer.Length, _CallbackReadMethod, null);
				}
				else
				{
					throw new Exception("Socket is closed, cannot Receive().");
				}
			}
			catch
			{
				throw;
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
			try
			{
				//check to be sure the network stream is valid before writing
				if (_NetworkStream.CanWrite)
				{
					_NetworkStream.EndWrite(state);
				}
			}
			catch
			{}
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
				//check the stream to be sure it is valid
				if (_NetworkStream.CanRead)
				{
					int bytesReceived = _NetworkStream.EndRead(state);

					//if there are bytes to process, do so.  Otherwise, the
					//connection has been lost, so clean it up
					if (bytesReceived > 0)
					{
						try
						{
							//send the incoming message to the message handler
							_MessageHandler(this);
						}
						finally
						{
							//start listening again
							Receive();
						}
					}
				}
			}
			catch
			{
				//the connection has been dropped so call the CloseHandler
				try
				{
					_SocketCloseHandler(this);
				}
				finally
				{
					Dispose();
				}
			}
		}
		#endregion private methods
	}
}
