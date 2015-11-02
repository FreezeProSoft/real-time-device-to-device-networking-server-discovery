using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace NetworkCommunication.Core
{
    /// <summary>
    /// Socket server.
    /// </summary>
    public class SocketBroadcastServer
    {
        /// <summary>
        /// Occurs when the server state changed.
        /// </summary>
        public event EventHandler<SocketServerState> StateChanged;

        /// <summary>
        /// Occurs when the server received message.
        /// </summary>
        public event EventHandler<ReceiveMessageEventArgs> ReceivedMessage;

        /// <summary>
        /// Gets the server current port.
        /// </summary>
        /// <value>The port.</value>
        public int Port
        {
            get
            {
                return port;
            }
        }

        /// <summary>
        /// Gets the server current state.
        /// </summary>
        /// <value>The state.</value>
        public SocketServerState State 
        {
            get
            {
                return state;
            }
        }

        /// <summary>
        /// Initializes a new instance of the server/> class.
        /// </summary>
        public SocketBroadcastServer()
        {
            this.state = SocketServerState.Stopped;
        }

        /// <summary>
        /// Run the server by specified port.
        /// </summary>
        /// <param name="port">The port that the server should listen.</param>
        public void Run(int port)
        {
            if (state == SocketServerState.Stopped)
            {
                this.port = port;

                OnStateChanged(SocketServerState.Starting);

                var thread = new Thread(Listening);

                thread.IsBackground = true;

                thread.Start();
            }
        }

        /// <summary>
        /// Stop this server instance.
        /// </summary>
        public void Stop()
        {
            if (mainSocket != null)
            {
                mainSocket.Close();
            
                mainSocket = null;
            }
        }

        /// <summary>
        /// Start listening the server socket.
        /// </summary>
        private void Listening()
        {
            RemoteHost host = null;
        
            try
            {  
                var address = new IPEndPoint(IPAddress.Any, port);

                mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                mainSocket.Bind(address);

                var hostAddress = address as EndPoint;

                OnStateChanged(SocketServerState.Running);

                while(state == SocketServerState.Running)
                {
                    byte[] buffer = new byte[1024];

                    var count = mainSocket.ReceiveFrom(buffer, ref hostAddress);

                    if(count > 0)
                    {
                        host = new RemoteHost(mainSocket, ((IPEndPoint)hostAddress).Address.ToString());
                        
                        OnReceivedMessage(host, buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                OnStateChanged(SocketServerState.Stopped);

                if (host != null && host.Connection != null && host.Connection.Connected)
                {
                    host.Connection.Close();
                }
            }
        }
            
        /// <summary>
        /// Raises the server state changed event.
        /// </summary>
        /// <param name="state">Server state.</param>
        protected void OnStateChanged(SocketServerState state)
        {
            this.state = state;
            
            var handler = StateChanged;

            if (handler != null)
            {
                handler(this, state);
            }
        }

        /// <summary>
        /// Raises the server received message event.
        /// </summary>
        /// <param name="host">Information about host connection.</param>
        /// <param name="message">Message byte array.</param>
        protected void OnReceivedMessage(RemoteHost host, byte[] message)
        {    
            var handler = ReceivedMessage;
        
            if (handler != null)
            {
                handler(this, new ReceiveMessageEventArgs(host, message));
            }
        }

        /// <summary>
        /// The main server socket.
        /// </summary>
        private Socket mainSocket;

        /// <summary>
        /// The current server state.
        /// </summary>
        private SocketServerState state;

        /// <summary>
        /// The current server port.
        /// </summary>
        private int port;
    }
}

