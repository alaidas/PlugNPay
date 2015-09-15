using System.Threading.Tasks;
using PlugNPayHub.PosControl.Messages;

namespace PlugNPayHub.Device.PedControl
{
    interface IPed
    {
        string PedId { get; }
        Task<ITransactionResponse> AuthorizeAsync(AuthorizeRequest authorizeRequest);
        Task<ITransactionResponse> ConfirmAsync(ConfirmRequest confirmRequest);
        Task<ITransactionResponse> ReversalAsync(ReversalRequest reversalRequest);
    }
}
