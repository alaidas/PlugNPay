using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NLog;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps
{
    class AsyncPosPedHub : IPedHub
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, AsyncPosPed> _peds = new Dictionary<string, AsyncPosPed>();
        private readonly Dictionary<string, Socket> _pedLinks = new Dictionary<string, Socket>();

        private Socket _pedListenSocket;

        public void Start(IPEndPoint pedListenEndpoint)
        {
            Ensure.NotNull(pedListenEndpoint, nameof(pedListenEndpoint));

            _pedListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _pedListenSocket.Bind(pedListenEndpoint);
            _pedListenSocket.Listen(10);

            AsyncCallback beginAccept = null;
            beginAccept = acceptResult =>
            {
                try
                {
                    Socket connectedPedSocket = _pedListenSocket.EndAccept(acceptResult);

                    byte[] lengthBuffer = new byte[2];
                    int readCount = 0;

                    AsyncCallback beginReceive = null;
                    beginReceive = receiveResult =>
                    {
                        try
                        {
                            readCount += connectedPedSocket.EndReceive(receiveResult);
                            if (readCount != 2)
                            {
                                connectedPedSocket.BeginReceive(lengthBuffer, readCount, lengthBuffer.Length, SocketFlags.None, beginReceive, null);
                                return;
                            }

                            int length = (lengthBuffer[0] << 8) + lengthBuffer[1];
                            if (length < 1 || length > 65280)
                                throw new Exception($"Invalid message length received [{length}]");

                            byte[] data = new byte[length];

                            readCount = 0;
                            while (readCount < data.Length)
                            {
                                connectedPedSocket.ReceiveTimeout = 2000;

                                int count = connectedPedSocket.Receive(data, readCount, data.Length - readCount, SocketFlags.None);
                                if (count == 0)
                                    throw new Exception("Socket closed on read operation");

                                readCount += count;
                            }

                            if (readCount != length)
                                throw new Exception("Cannot receive full message");

                            try
                            {
                                Process(connectedPedSocket, data);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }

                            connectedPedSocket.BeginReceive(lengthBuffer, readCount = 0, lengthBuffer.Length, SocketFlags.None, beginReceive, null);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);

                            try
                            {
                                connectedPedSocket.Shutdown(SocketShutdown.Both);
                                connectedPedSocket.Close();
                            }
                            catch { }
                        }
                    };

                    connectedPedSocket.BeginReceive(lengthBuffer, 0, lengthBuffer.Length, SocketFlags.None, beginReceive, null);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    _pedListenSocket.BeginAccept(beginAccept, null);
                }
            };

            _pedListenSocket.BeginAccept(beginAccept, null);
        }

        public IPed RegisterPed(string pedId, byte[] apak)
        {
            Ensure.NotNull(pedId, nameof(pedId));
            Ensure.NotNull(apak, nameof(apak));

            return _peds[pedId] = new AsyncPosPed(pedId, apak, this);
        }

        public void SendData(string pedId, byte[] data)
        {
            Ensure.NotNull(pedId, nameof(pedId));
            Ensure.NotNull(data, nameof(data));

            Socket pedSocket;
            if (!_pedLinks.TryGetValue(pedId, out pedSocket))
                throw new Exception($"Ped '{pedId}' socket is not found");

            pedSocket.Send(data);
        }

        private void Process(Socket pedSocket, byte[] receivedData)
        {
            Ensure.NotNull(receivedData, nameof(receivedData));

            string pedId = AsyncPosPacket.GetPosId(receivedData);
            if (string.IsNullOrEmpty(pedId))
                throw new Exception($"Cannot process received data without {nameof(pedId)}");

            AsyncPosPed ped;
            if (!_peds.TryGetValue(pedId, out ped))
                throw new Exception($"{pedId} is not found");

            _pedLinks[pedId] = pedSocket;

            ped.Process(receivedData);
        }
    }
}
