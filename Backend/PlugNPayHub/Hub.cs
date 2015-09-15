using System;
using System.Net;
using NLog;
using PlugNPayHub.Device.PedControl;
using PlugNPayHub.Device.PedControl.Eps;
using PlugNPayHub.Device.PrinterControl;
using PlugNPayHub.PosControl;
using PlugNPayHub.Utils;

namespace PlugNPayHub
{
    public class Hub
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private AsyncPosPedHub _terminalsHub;
        private PosHub _posController;
        private FiscalPrinterHub _printerHub;

        public void Start(IPEndPoint asyncPosEndpoint, string posControllerBindAddress, IPEndPoint fiscalPrinterEndpoint)
        {
            try
            {
                _terminalsHub = new AsyncPosPedHub();
                _terminalsHub.Start(asyncPosEndpoint);
                IPed ped = _terminalsHub.RegisterPed("LTRM01-01", Hex.Decode("78FDE03BE7BECC276847C5761C69FEC8"));
                IPed ped2 = _terminalsHub.RegisterPed("LOYALTY1", Hex.Decode("C6478C69D0B209972F184350D4A4162F"));

                _printerHub = new FiscalPrinterHub();
                _printerHub.Start(fiscalPrinterEndpoint);
                FiscalPrinter printer = _printerHub.RegisterPrinter("TESTPOSP");

                _posController = new PosHub();
                _posController.Start(posControllerBindAddress);
                _posController.RegisterPos(new Pos("TESTPOS", printer, ped));
                _posController.RegisterPos(new Pos("TESTPOS2", printer, ped2));

                Log.Info("Service is started");
            }
            catch(Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public void Stop()
        {
        }
    }
}
