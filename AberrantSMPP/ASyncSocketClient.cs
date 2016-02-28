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
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Common.Logging;

//#define HARDCORE_LOGGING

namespace AberrantSMPP
{
    /// <summary>
    ///     Socket class for asynchronous connection.
    /// </summary>
    internal sealed class AsyncSocketClient : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region delegates

        /// <summary>
        ///     Called when a message is received.
        /// </summary>
        /// <param name="asyncSocketClient">
        ///     The AsyncSocketClient to receive messages from.
        /// </param>
        public delegate void MessageHandler(AsyncSocketClient asyncSocketClient);

        /// <summary>
        ///     Called when a connection is closed.
        /// </summary>
        /// <param name="asyncSocketClient">
        ///     The AsyncSocketClient to receive messages from.
        /// </param>
        public delegate void SocketClosingHandler(
            AsyncSocketClient asyncSocketClient);

        /// <summary>
        ///     Called when a socket error occurs.
        /// </summary>
        /// <param name="asyncSocketClient">
        ///     The AsyncSocketClient to receive messages from.
        /// </param>
        /// <param name="exception">
        ///     The exception that generated the error.
        /// </param>
        public delegate void ErrorHandler(
            AsyncSocketClient asyncSocketClient, Exception exception);

        #endregion delegates

        //private const int BUFFER_SIZE = 1048576; // One MB
        private const int BufferSize = 262144;

        #region private fields

        private readonly ReaderWriterLockSlim _socketLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly Queue<byte[]> _sendQueue = new Queue<byte[]>();
        private TcpClient _tcpClient;
        private readonly AsyncCallback _callbackReadMethod;
        private readonly AsyncCallback _callbackWriteMethod;
        private readonly MessageHandler _messageHandler;
        private readonly SocketClosingHandler _socketCloseHandler;
        private ErrorHandler _errorHandler;
        private Stream _stream;
        private bool _isDisposed;
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
        private bool _sendPending;
        private bool _useSsl;
#endif

        #endregion private fields

        #region properties

        private static string ThreadId => $"{Thread.CurrentThread.Name} ({Thread.CurrentThread.ManagedThreadId})";

        /// <summary>
        ///     The server address to connect to.
        /// </summary>
        public string ServerAddress { get; private set; }

        /// <summary>
        ///     The server port to connect to.
        /// </summary>
        public ushort ServerPort { get; private set; }

        /// <summary>
        ///     A user set state object to associate some state with a connection.
        /// </summary>
        public object StateObject { get; set; }

        /// <summary>
        ///     Buffer to hold data coming in from the socket.
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="AsyncSocketClient" /> is connected.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool Connected
        {
            get
            {
                using (new ReadOnlyLock(_socketLock))
                {
                    return _tcpClient != null && _tcpClient.Connected && _tcpClient.Client.Connected;
                }
            }
        }

        #endregion properties

        /// <summary>
        ///     Constructs an AsyncSocketClient.
        /// </summary>
        /// <param name="bufferSize">The size of the receive buffer.</param>
        /// <param name="stateObject">
        ///     The object to use for sending state
        ///     information.
        /// </param>
        /// <param name="msgHandler">
        ///     The user-defined message handling method.
        /// </param>
        /// <param name="closingHandler">
        ///     The user-defined socket closing
        ///     handling method.
        /// </param>
        /// <param name="errHandler">
        ///     The user defined error handling method.
        /// </param>
        public AsyncSocketClient(int bufferSize, object stateObject,
            MessageHandler msgHandler, SocketClosingHandler closingHandler,
            ErrorHandler errHandler)
        {
            Log.DebugFormat("Initializing new instance ({0}).", GetHashCode());

            Buffer = new byte[BufferSize];
            StateObject = stateObject;

            //set handlers
            _messageHandler = msgHandler;
            _socketCloseHandler = closingHandler;
            _errorHandler = errHandler;

            //set the asynchronous method handlers
            _callbackReadMethod = ReceiveComplete;
            _callbackWriteMethod = SendComplete;

            //haven't been disposed yet
            _isDisposed = false;
        }

        /// <summary>
        ///     Finalizer method.  If Dispose() is called correctly, there is nothing
        ///     for this to do.
        /// </summary>
        ~AsyncSocketClient()
        {
            if (!_isDisposed)
            {
                Dispose();
            }
        }

        #region public methods

        /// <summary>
        ///     Sets the disposed flag to true and disconnects the socket.
        /// </summary>
        public void Dispose()
        {
            using (new WriteLock(_socketLock))
            {
                try
                {
                    Log.DebugFormat("Disposing instance ({0}).", GetHashCode());
                    _isDisposed = true;
                    Disconnect();
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception thrown while disposing.", ex);
                }
            }
        }

        /// <summary>
        ///     Connects the socket to the given IP address and port.
        ///     This also calls Receive().
        /// </summary>
        /// <param name="address">The IP address of the server.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="useSsl"></param>
        public void Connect(string address, ushort port, bool useSsl)
        {
            using (new WriteLock(_socketLock))
            {
                //do we already have an open connection?
                if (_tcpClient != null)
                    throw new InvalidOperationException("Already connected to remote host.");

                Log.DebugFormat("Connecting instance ({0}) to {1}:{2}.", GetHashCode(), address, port);

                ServerAddress = address;
                ServerPort = port;
                _useSsl = useSsl;
                //attempt to establish the connection
                _tcpClient = new TcpClient(ServerAddress, ServerPort);

                //set some socket options
                _tcpClient.ReceiveBufferSize = BufferSize;
                _tcpClient.SendBufferSize = BufferSize;
                _tcpClient.NoDelay = true;
                //if the connection is dropped, drop all associated data
                _tcpClient.LingerState = new LingerOption(false, 0);
            }

            Stream stream = _tcpClient.GetStream();

            if (_useSsl)
            {
                var sslStream = new SslStream(_tcpClient.GetStream(), false,
                    (sender, certificate, chain, errors) => true);
                sslStream.AuthenticateAsClient(ServerAddress);
                stream = sslStream;
            }

            _stream = stream;
            //start receiving messages
            Receive();
        }

        /// <summary>
        ///     Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            using (new WriteLock(_socketLock))
            {
                Log.DebugFormat("Disconnecting instance ({0}).", GetHashCode());

                _stream.Dispose();

                //close down the connection, making sure it exists first
                if (_tcpClient != null)
                {
                    Helper.ShallowExceptions(() => _tcpClient.Close());
                }

                Helper.ShallowExceptions(() => _sendQueue.Clear());

                //prep for garbage collection-we may want to use this instance again
                _tcpClient = null;
            }
        }

        /// <summary>
        ///     Asynchronously sends data across the socket.
        /// </summary>
        /// <param name="buffer">
        ///     The buffer of data to send.
        /// </param>
        public void Send(byte[] buffer)
        {
            using (new ReadOnlyLock(_socketLock))
            {
#if HARDCORE_LOGGING
				_Log.DebugFormat("Instance {0} - {1} => Sending..", this.GetHashCode(), _ThreadId);
#endif

                if (!Connected)
                    throw new IOException("Socket is closed, cannot Send().");

                lock (_sendQueue)
                {
                    if (_sendPending)
                    {
                        Log.DebugFormat("Instance {0} - {1} => Queuing data for transmission..", GetHashCode(),
                            ThreadId);
                        _sendQueue.Enqueue(buffer);
                    }
                    else
                    {
#if HARDCORE_LOGGING
						_Log.DebugFormat("Instance {0} - {1} => Sending actual data..", this.GetHashCode(), _ThreadId);
#endif

                        //send the data; don't worry about receiving any state information back;
                        _sendPending = true;


                        var stream = _stream;

                        stream.BeginWrite(buffer, 0, buffer.Length, _callbackWriteMethod, null);
#if HARDCORE_LOGGING
						_Log.DebugFormat("Instance {0} - {1} => Data sent..", this.GetHashCode(), _ThreadId);
#endif
                    }
                }
            }
        }

        /// <summary>
        ///     Asynchronously receives data from the socket.
        /// </summary>
        // XXX: Method is now private as it's called internally only. (pruiz)
        private void Receive()
        {
            using (new ReadOnlyLock(_socketLock))
            {
                if (!Connected)
                    throw new IOException("Socket is closed, cannot Receive().");

                Array.Clear(Buffer, 0, Buffer.Length); // Clear contents..

                var stream = _stream;


                stream.BeginRead(Buffer, 0, Buffer.Length, _callbackReadMethod, null);
            }
        }

        #endregion public methods

        #region private methods

        /// <summary>
        ///     Callback method called by the NetworkStream's thread when a message
        ///     is sent.
        /// </summary>
        /// <param name="state">
        ///     The state object holding information about
        ///     the connection.
        /// </param>
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

                    if (!_sendPending)
                        Log.ErrorFormat("Instance {0} - {1} => SendComplete called while SendPending=False?!?!?",
                            GetHashCode(), ThreadId);

                    _sendPending = false;

                    if (_tcpClient != null)
                    {
                        _stream.EndWrite(state);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(string.Format("Instance {0} - {1} => Async send failed.", GetHashCode(), ThreadId), ex);
            }

            // If there are more packets to send..

            lock (_sendQueue)
            {
#if HARDCORE_LOGGING
				_Log.DebugFormat("Instance {0} - {1} => Processing send queue.", this.GetHashCode(), _ThreadId);
#endif

                if (_sendQueue.Count == 0)
                {
#if HARDCORE_LOGGING
					_Log.DebugFormat("Instance {0} - {1} => No packets on queue..", this.GetHashCode(), _ThreadId);
#endif
                    // Reduce queue internal space..
                    _sendQueue.TrimExcess();
                    return;
                }

#if HARDCORE_LOGGING
				_Log.DebugFormat("Instance {0} - {1} => Sending queued packet.", this.GetHashCode(), _ThreadId);
#endif

                // Send another packet..
                using (new ReadOnlyLock(_socketLock))
                {
                    _sendPending = true;
                    var packet = _sendQueue.Dequeue();
                    _stream.BeginWrite(packet, 0, packet.Length, _callbackWriteMethod, null);
                }
            }
        }

        /// <summary>
        ///     Callback method called by the NetworkStream's thread when a message
        ///     arrives.
        /// </summary>
        /// <param name="state">
        ///     The state object holding information about
        ///     the connection.
        /// </param>
        private void ReceiveComplete(IAsyncResult state)
        {
            try
            {
                var bytesReceived = 0;

                using (new ReadOnlyLock(_socketLock))
                {
                    if (_tcpClient != null)
                        bytesReceived = _stream.EndRead(state);
                }

                //if there are bytes to process, do so.  Otherwise, the
                //connection has been lost, so clean it up
                if (bytesReceived > 0)
                {
                    try
                    {
                        //send the incoming message to the message handler
                        _messageHandler(this);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            string.Format("Instance {0} - {1} => Receive message handler failed.", GetHashCode(),
                                ThreadId), ex);
                    }
                }

                // XXX: If readBytesCount == 0 --> Connection has been closed.

                // XXX: Access _tcpClient.Client.Available to ensure socket is still working..

                //start listening again
                Receive();
            }
            catch (Exception ex)
            {
                Log.Warn("Receive failed.", ex);

                //the connection has been dropped so call the CloseHandler
                try
                {
                    _socketCloseHandler(this);
                }
                finally
                {
                    Log.WarnFormat("Instance {0} - {1} => Connection terminated, disposing..", GetHashCode(), ThreadId);
                    Dispose();
                }
            }
        }

        #endregion private methods
    }
}