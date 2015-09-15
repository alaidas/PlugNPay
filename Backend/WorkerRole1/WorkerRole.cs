using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using PlugNPayHub;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private Hub _hub;

        public override void Run()
        {
            if (_hub != null) return;

            IPEndPoint asyncPosPedControllerEndPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["AsyncPosPedHub"].IPEndpoint;

            RoleInstanceEndpoint posControllerInstanceEndPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["PosController"];
            string posControllerBindAddress = $"{posControllerInstanceEndPoint.Protocol}://{posControllerInstanceEndPoint.IPEndpoint}/";

            IPEndPoint fiscalPrinterHubEndPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["FiscalPrinterHub"].IPEndpoint;

            _hub = new Hub();
            _hub.Start(asyncPosPedControllerEndPoint, posControllerBindAddress, fiscalPrinterHubEndPoint);

            while (true)
                Thread.Sleep(1000);
        }


        public override void OnStop()
        {
            _hub?.Stop();
            base.OnStop();
        }
    }
}