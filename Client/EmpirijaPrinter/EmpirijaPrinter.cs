using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Controllers.EmpirijaPrinter.Messages;
using PlugNPay.Utils;
using PlugNPay.Utils.Logs;
using PlugNPayClient.Controller;
using PlugNPayClient.Controller.Data;

namespace Controllers.EmpirijaPrinter
{
    public class EmpirijaPrinter : IController, IFiscalPrinter
    {
        public string Id => nameof(EmpirijaPrinter);

        private ControllerContext _context;
        private dynamic _empirijaPrinter;

        private IPEndPoint _printerHubEndPoint;
        private Socket _printerHubSocket;

        private string _printerId;

        public void Starup(ControllerContext context)
        {
            Ensure.NotNull(context, nameof(context));
            _context = context;

            //string dynamicObjectName;
            //if (!context.StartupAttributes.TryGetValue("class", out dynamicObjectName) || string.IsNullOrEmpty(dynamicObjectName))
            //    throw new ArgumentNullException($"Controller {Id} missing \"class\" parameter");

            //Type emipirijaType = Type.GetTypeFromProgID(dynamicObjectName);
            //if (emipirijaType == null)
            //    throw new Exception($"Cannot get type {dynamicObjectName}");

            //_empirijaPrinter = Activator.CreateInstance(emipirijaType);
            //if (_empirijaPrinter == null)
            //    throw new Exception($"Cannot initialize dynamic object {emipirijaType.FullName}");

            if (!context.StartupAttributes.TryGetValue("printerId", out _printerId) || string.IsNullOrEmpty(_printerId))
                throw new ArgumentNullException($"Controller {Id} missing \"printerId\" parameter");

            string printerHubEndpoint;
            if (!context.StartupAttributes.TryGetValue("printerHubEndpoint", out printerHubEndpoint) || string.IsNullOrEmpty(printerHubEndpoint))
                throw new ArgumentNullException($"Controller {Id} is missing \"printerHubEndpoint\" parameter");

            string[] endPointArgs = printerHubEndpoint.Split(':');
            if (endPointArgs.Length != 2)
                throw new ArgumentNullException($"Controller {Id} \"printerHubEndpoint\" parameter has incorrect data. Format should be xxx.xxx.xxx.xxx:pppp");

            _printerHubEndPoint = new IPEndPoint(IPAddress.Parse(endPointArgs[0]), int.Parse(endPointArgs[1]));

            BeginMonitorHubConnection();
        }

        private bool _shutdown;
        public void Shutdown()
        {
            _shutdown = true;
        }

        public void PrintTest()
        {
            _empirijaPrinter.BeginNonFiscalReceipt();

            _empirijaPrinter.PrintNonFiscalLine("PlugNPay :)", 0x48);
            _empirijaPrinter.PrintNonFiscalLine("", 0x41);

            _empirijaPrinter.EndNonFiscalReceipt();

            _context.Log.LogMessage("Test printed");
        }

        public void PrintEndOfReceipt()
        {
            throw new NotImplementedException();
        }

        public void PrintProductLine(Product product)
        {
            throw new NotImplementedException();
        }

        public void PrintTextLine(string line)
        {
            throw new NotImplementedException();
        }

        public void PrintXReport()
        {
            _empirijaPrinter.PrintMiniXReport();
        }

        public void PrintZReport()
        {
            throw new NotImplementedException();
        }

        private void PrintPaymentData(PrintData paymentData)
        {

        }

        #region Hub communication

        private void ConnectToHub(IPEndPoint printerHubEndpoint)
        {
            Ensure.NotNull(printerHubEndpoint, nameof(printerHubEndpoint));

            _printerHubSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _printerHubSocket.Connect(printerHubEndpoint);


            byte[] messageLength = new byte[2];
            int receiveCount = 0;

            AsyncCallback receiveCallback = null;
            receiveCallback = (receiveResult) =>
            {
                try
                {
                    receiveCount += _printerHubSocket.EndReceive(receiveResult);
                    if (receiveCount != 2)
                    {
                        _printerHubSocket.BeginReceive(messageLength, receiveCount, messageLength.Length, SocketFlags.None, receiveCallback, null);
                        return;
                    }

                    int length = (messageLength[0] << 8) + messageLength[1];
                    if (length < 1 || length > 65280)
                        throw new Exception($"Invalid message length received [{length}]");

                    byte[] data = new byte[length];

                    int readCount = 0;
                    while (readCount < data.Length)
                    {
                        _printerHubSocket.ReceiveTimeout = 2000;

                        int count = _printerHubSocket.Receive(data, readCount, data.Length - readCount, SocketFlags.None);
                        if (count == 0)
                            throw new Exception("Socket closed on read operation");

                        readCount += count;
                    }

                    if (readCount != length)
                        throw new Exception("Cannot receive full message");

                    try
                    {
                        Process(data);
                    }
                    catch (Exception ex)
                    {
                        _context.Log.LogError(ex);
                    }

                    _printerHubSocket.BeginReceive(messageLength, receiveCount = 0, messageLength.Length, SocketFlags.None, receiveCallback, null);
                }
                catch (Exception ex)
                {
                    _context.Log.LogError(ex);

                    try
                    {
                        _printerHubSocket.Shutdown(SocketShutdown.Both);
                        _printerHubSocket.Close();
                    }
                    catch { }
                }
            };

            _printerHubSocket.BeginReceive(messageLength, 0, messageLength.Length, SocketFlags.None, receiveCallback, null);

            Request request = new Request
            {
                PrinterId = _printerId,
                RequestId = Guid.NewGuid().ToString(),
                RequestType = "Initialization",
                Content = JsonConverter.Serialize(new Initialize())
            };

            _printerHubSocket.Send(Network.CreatePacket(JsonConverter.Serialize(request)));
        }

        private void Process(byte[] receivedData)
        {
            Ensure.NotNull(receivedData, nameof(receivedData));

            _context.Log.LogMessage($"Received data: {Encoding.UTF8.GetString(receivedData)}");

            Request request = JsonConverter.Deserialize<Request>(receivedData);
            if (request.PrinterId != _printerId)
            {
                _context.Log.LogMessage($"Received request PrinterId {request.PrinterId} does not match configured {_printerId}");

                Request response = new Request
                {
                    PrinterId = _printerId,
                    RequestId = request.RequestId,
                    RequestType = "NAK"
                };

                _printerHubSocket.Send(Network.CreatePacket(JsonConverter.Serialize(response)));
            }

            try
            {
                string response = ProcessRequest(request);
                _printerHubSocket.Send(Network.CreatePacket(response));
            }
            catch(Exception ex)
            {
                _context.Log.LogError(ex);

                Request response = new Request
                {
                    PrinterId = _printerId,
                    RequestId = request.RequestId,
                    RequestType = "NAK"
                };

                _printerHubSocket.Send(Network.CreatePacket(JsonConverter.Serialize(response)));
            }
        }

        private string ProcessRequest(Request receivedRequest)
        {
            Ensure.NotNull(receivedRequest, nameof(receivedRequest));

            switch (receivedRequest.RequestType.ToLower())
            {
                case "printpaymentdata":
                    {
                        PrintData printData = JsonConverter.Deserialize<PrintData>(receivedRequest.Content);
                        if (printData == null)
                            throw new Exception($"Cannot deserialize '{receivedRequest.RequestType}' content");

                        PrintPaymentData(printData);

                        Request response = new Request
                        {
                            PrinterId = _printerId,
                            RequestId = receivedRequest.RequestId,
                            RequestType = "ACK"
                        };

                        return JsonConverter.Serialize(response);
                    }

                default:
                    throw new NotSupportedException($"Request '{receivedRequest.RequestType}' is not supported");
            }
        }

        private void BeginMonitorHubConnection()
        {
            new Thread(o =>
            {
                while (!_shutdown)
                {
                    try
                    {
                        if (_printerHubSocket != null && _printerHubSocket.Connected)
                            continue;

                        ConnectToHub(_printerHubEndPoint);
                    }
                    catch (Exception ex)
                    {
                        _context.Log.LogError(ex);
                    }
                    finally
                    {
                        Thread.Sleep(1000);
                    }
                }
            })
            { IsBackground = true }.Start();
        }

        #endregion
    }
}
