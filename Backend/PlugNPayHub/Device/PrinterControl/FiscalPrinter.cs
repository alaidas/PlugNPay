using System;
using System.Threading.Tasks;
using PlugNPayHub.Device.PrinterControl.Messages;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PrinterControl
{
    class FiscalPrinter
    {
        public string FiscalPrinterId { get; private set; }

        private readonly Func<PrintData, Task> _onPrintCardPaymentReceipt;

        public FiscalPrinter(string fiscalPrinterId, Func<PrintData, Task> onPrintCardPaymentReceipt)
        {
            Ensure.NotNull(fiscalPrinterId, nameof(fiscalPrinterId));
            Ensure.NotNull(onPrintCardPaymentReceipt, nameof(onPrintCardPaymentReceipt));

            FiscalPrinterId = fiscalPrinterId;
            _onPrintCardPaymentReceipt = onPrintCardPaymentReceipt;
        }

        public async Task PrintMerchantReceipt(string text)
        {
            await _onPrintCardPaymentReceipt(new PrintData { Text = text });
        }

        public async Task PrintClientReceipt(string text)
        {
            await _onPrintCardPaymentReceipt(new PrintData { Text = text });
        }
    }
}
