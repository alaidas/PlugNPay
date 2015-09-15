namespace PlugNPayHub.Device.PedControl
{
    interface IPedHub
    {
        void SendData(string pedId, byte[] data);
    }
}
