using System;
using System.Threading.Tasks;
using PlugNPayHub.Device.PedControl;
using PlugNPayHub.Device.PrinterControl;
using PlugNPayHub.PosControl.Messages;
using PlugNPayHub.Utils;

namespace PlugNPayHub.PosControl
{
    class Pos
    {
        public string PosId { get; private set; }

        private readonly FiscalPrinter _printer;
        private readonly IPed _ped;

        public Pos(string posId, FiscalPrinter fiscalPrinter, IPed ped)
        {
            Ensure.NotNull(posId, nameof(posId));
            Ensure.NotNull(fiscalPrinter, nameof(fiscalPrinter));
            Ensure.NotNull(ped, nameof(_ped));

            PosId = posId;
            _printer = fiscalPrinter;
            _ped = ped;
        }

        public void AddProduct(Product product)
        {
            Ensure.NotNull(product, nameof(product));

            throw new NotImplementedException();
        }

        public void RemoveProduct(string productId)
        {
            throw new NotImplementedException();
        }

        public void CancelReceipt()
        {
            throw new NotImplementedException();
        }

        public void PayInCash(long amount)
        {
            throw new NotImplementedException();
        }

        public async Task<IResponse> AuthorizeAsync(AuthorizeRequest authorizeRequest)
        {
            Ensure.NotNull(authorizeRequest, nameof(authorizeRequest));

            AuthorizeResponse result = await _ped.AuthorizeAsync(authorizeRequest) as AuthorizeResponse;
            if (result == null)
                throw new Exception("Cannot authorize payment");

            try
            {
                if (!string.IsNullOrEmpty(result.MerchantReceipt))
                    await _printer.PrintMerchantReceipt(result.MerchantReceipt);

                if (!string.IsNullOrEmpty(result.ClientReceipt))
                    await _printer.PrintMerchantReceipt(result.ClientReceipt);
            }
            catch
            {
                await _ped.ReversalAsync(new ReversalRequest { TransactionId = authorizeRequest.TransactionId });
                throw;
            }

            return result;
        }

        public async Task<IResponse> ConfirmAsync(ConfirmRequest confirmRequest)
        {
            Ensure.NotNull(confirmRequest, nameof(confirmRequest));

            IResponse result = await _ped.ConfirmAsync(confirmRequest);
            if (result == null)
                throw new Exception("Cannot confirm payment");

            return result;
        }

        public void FinishReceipt()
        {
            throw new NotImplementedException();
        }
    }
}
