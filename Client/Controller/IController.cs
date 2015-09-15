namespace PlugNPayClient.Controller
{
    public interface IController
    {
        string Id { get; }
        void Starup(ControllerContext context);
        void Shutdown();
    }
}
