using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace NetworkCommunication.Core
{
    /// <summary>
    /// Socket client.
    /// </summary>
    public class SocketBroadcastClient
    {
        /// <summary>
        /// Gets the client current port.
        /// </summary>
        /// <value>The client current port.</value>
        public int Port
        {
            get
            {
                return port;
            }
        }

        /// <summary>
        /// The client dispose state.
        /// </summary>
        public bool IsDisposed
        {
            get
            { 
                return isDisposed;
            }
        }
 
        /// <summary>
        /// Initializes a new instance of the broadcast client.
        /// </summary>
        /// <param name="port">Port.</param>
        public SocketBroadcastClient(int port)
        {
            this.port = port;

            try
            {
                var address = new IPEndPoint(IPAddress.Broadcast, port);

                mainSocket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                mainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

                mainSocket.Connect(address);

                messageQueue = new QueueWithBlock<byte[]>();

                messagesThread = new Thread(MessagesThreadWork);

                messagesThread.IsBackground = true;

                messagesThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="message">The byte array.</param>
        public void SendMessage(byte[] message)
        {
            messageQueue.Enqueue(message);
        }

        /// <summary>
        /// Start loop for sending messages.
        /// </summary>
        private void MessagesThreadWork()
        {
            while (!isDisposed)
            {
                try
                {
                    var message = messageQueue.Dequeue();

                    lock (lockObject)
                    {
                        if (mainSocket != null && message != null)
                        {
                            mainSocket.Send(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
            
        public void Dispose()
        {
            lock (lockObject)
            {
                isDisposed = true;

                if (messageQueue != null)
                {
                    messageQueue.Release();

                    messageQueue.Clear();

                    messageQueue = null;
                }

                if (mainSocket != null)
                {
                    mainSocket.Close();

                    mainSocket.Dispose();

                    mainSocket = null;
                }

                messagesThread = null;
            }

            lockObject = null;
        }
           
        /// <summary>
        /// The message queue.
        /// </summary>
        private QueueWithBlock<byte[]> messageQueue;

        /// <summary>
        /// Thread for sending messages
        /// </summary>
        private Thread messagesThread;

        /// <summary>
        /// The main client socket.
        /// </summary>
        private Socket mainSocket;

        /// <summary>
        /// The client current port.
        /// </summary>
        private int port;

        /// <summary>
        /// The client dispose state.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// The lock object for synchronization.
        /// </summary>
        private object lockObject = new object();
    }
}

