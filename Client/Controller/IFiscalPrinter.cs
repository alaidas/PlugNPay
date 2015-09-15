using PlugNPayClient.Controller.Data;

namespace PlugNPayClient.Controller
{
    public interface IFiscalPrinter
    {
        void PrintProductLine(Product product);
        void PrintTextLine(string line);
        void PrintEndOfReceipt();

        void PrintXReport();
        void PrintZReport();
    }
}
