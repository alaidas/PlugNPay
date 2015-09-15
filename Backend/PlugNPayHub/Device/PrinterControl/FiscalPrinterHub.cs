using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Fluent;
using PlugNPayHub.Device.PrinterControl.Messages;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PrinterControl
{
    class FiscalPrinterHub
    {
        private static readonly Encoding CurrentEncoding = Encoding.UTF8;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Socket _listenSocket;
        private readonly Dictionary<string, FiscalPrinter> _printers = new Dictionary<string, FiscalPrinter>();
        private readonly Dictionary<string, Socket> _printerLink = new Dictionary<string, Socket>();

        public void Start(IPEndPoint ficalPrinterEndpoint)
        {
            Ensure.NotNull(ficalPrinterEndpoint, nameof(ficalPrinterEndpoint));

            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(ficalPrinterEndpoint);
            _listenSocket.Listen(2);

            AsyncCallback acceptCallback = null;
            acceptCallback = (acceptResult) =>
            {
                try
                {
                    Socket client = _listenSocket.EndAccept(acceptResult);

                    byte[] messageLength = new byte[2];
                    int receiveCount = 0;

                    AsyncCallback receiveCallback = null;
                    receiveCallback = (receiveResult) =>
                    {
                        try
                        {
                            receiveCount += client.EndReceive(receiveResult);
                            if (receiveCount != 2)
                            {
                                client.BeginReceive(messageLength, receiveCount, messageLength.Length, SocketFlags.None, receiveCallback, null);
                                return;
                            }

                            int length = (messageLength[0] << 8) + messageLength[1];
                            if (length < 1 || length > 65280)
                                throw new Exception($"Invalid message length received [{length}]");

                            byte[] data = new byte[length];

                            int readCount = 0;
                            while (readCount < data.Length)
                            {
                                client.ReceiveTimeout = 2000;

                                int count = client.Receive(data, readCount, data.Length - readCount, SocketFlags.None);
                                if (count == 0)
                                    throw new Exception("Socket closed on read operation");

                                readCount += count;
                            }

                            if (readCount != length)
                                throw new Exception("Cannot receive full message");

                            try
                            {
                                ProcessAsync(client, data);
                            }
                            catch(Exception ex)
                            {
                                Log.Error(ex);
                            }

                            client.BeginReceive(messageLength, receiveCount = 0, messageLength.Length, SocketFlags.None, receiveCallback, null);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);

                            try
                            {
                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                            }
                            catch { }
                        }
                    };

                    client.BeginReceive(messageLength, 0, messageLength.Length, SocketFlags.None, receiveCallback, null);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    _listenSocket.BeginAccept(acceptCallback, null);
                }
            };

            _listenSocket.BeginAccept(acceptCallback, null);

        }

        public void Stop()
        {
            try
            {
                _listenSocket?.Shutdown(SocketShutdown.Both);

                foreach (var kv in _printerLink)
                {
                    try
                    {
                        kv.Value.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                }

                _printerLink.Clear();
            }
            catch { }
        }

        public FiscalPrinter RegisterPrinter(string printerId)
        {
            Ensure.NotNull(printerId, nameof(printerId));

            FiscalPrinter printer;
            if (!_printers.TryGetValue(printerId, out printer))
                _printers[printerId] = printer = new FiscalPrinter(printerId, async (printData) => await PrintPaymentReceipt(printerId, printData));

            return printer;
        }

        private void ProcessAsync(Socket printerSocket, byte[] receivedData)
        {
            Log.Info($"Received message: {Encoding.UTF8.GetString(receivedData)}");

            Request request = Converter.Deserialize<Request>(receivedData);
            if (request == null)
                throw new Exception($"Received request cannot be deserialized: {receivedData}");

            if (string.IsNullOrEmpty(request.PrinterId))
                throw new Exception($"Received request missing '{nameof(request.PrinterId)}': {CurrentEncoding.GetString(receivedData)}");

            FiscalPrinter printer;
            if (!_printers.TryGetValue(request.PrinterId, out printer) || printer == null)
                throw new Exception($"Printer '{request.PrinterId}' is not found");

            _printerLink[request.PrinterId] = printerSocket;

            if (string.IsNullOrEmpty(request.RequestId))
                throw new Exception($"Received request missing '{nameof(request.RequestId)}': {CurrentEncoding.GetString(receivedData)}");

            ProcessAction(request.RequestType, printer, request.RequestId, request.Content);
        }

        private readonly EventsMonitor<string> _eventsMonitor = new EventsMonitor<string>();

        private void ProcessAction(string requestType, FiscalPrinter printer, string requestId, string message)
        {
            switch (requestType.ToLower())
            {
                case "ack":
                case "nak":
                    _eventsMonitor.FireEvent(Network.CreateHeader(requestType, printer.FiscalPrinterId, requestId), requestType.ToLower());
                    break;
            }
        }

        private async Task PrintPaymentReceipt(string printerId, PrintData printData)
        {
            Ensure.NotNull(printerId, nameof(printerId));
            Ensure.NotNull(printData, nameof(printData));

            Socket link;
            if (!_printerLink.TryGetValue(printerId, out link) || link == null)
                throw new Exception($"Printer '{printerId}' link is not found");

            if (!link.Connected)
                throw new Exception($"Printer '{printerId}' is not connected");

            Request printRequest = new Request
            {
                PrinterId = printerId,
                RequestId = Guid.NewGuid().ToString(),
                RequestType = "PrintPaymentData",
                Content = Converter.Serialize(printData)
            };

            link.Send(Network.CreatePacket(Converter.Serialize(printRequest)));

            var result = await _eventsMonitor.WaitOneAsync(Network.CreateHeader("ACK", printRequest.PrinterId, printRequest.RequestId), 1000 * 60);
            if (result == null)
                throw new Exception("Print result is not received");
        }
    }
}
