using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps
{
    public enum TransactionStates
    {
        NotStarted = 0,
        Authorizing = 1,
        Approved = 2,
        Declined = 3,
        Confirming = 4,
        Confirmed = 5,
        Reversing = 6,
        Reversed = 7,
        Voiding = 8,
        Voided = 9
    }

    public class TransactionState
    {
        public DateTime Time { get; set; }
        public string DocumentNumber { get; set; }
        public long Amount { get; set; }
        public long Cash { get; set; }
        public int Currency { get; set; }
        public string Language { get; set; }
        public string AuthorizationId { get; set; }
        public string Last4CardNumberDigits { get; set; }
        public bool PreAuthorize { get; set; }

        private TransactionStates _state;
        public TransactionStates State
        {
            get { return _state; }
            set
            {
                _state = value;

                AutoSave();
            }
        }

        public string InformationText { get; set; }
        public string AuthorizationCode { get; set; }
        public string Rrn { get; set; }
        public string Stan { get; set; }
        public string CardType { get; set; }
        public string CardToken { get; set; }
        public List<Receipt> Receipts { get; set; }

        private AutoSaver _autoSaver;
        private readonly Semaphore _autoSaveContext = new Semaphore(1, 1);
        private readonly object _saveLock = new object();

        public TransactionState()
        {
            Receipts = new List<Receipt>();
        }

        public string GetMerchantReceipt()
        {
            if (Receipts.Count == 0) return null;

            Receipt rcpt = Receipts.FirstOrDefault(r => r.Flags.IsFlagSet("MC"));
            return rcpt == null ? null : FormatReceipt(rcpt.Text);
        }

        public string GetCustumerReceipt()
        {
            if (Receipts.Count == 0) return null;

            Receipt rcpt = Receipts.FirstOrDefault(r => !r.Flags.IsFlagSet("MC"));
            return rcpt == null ? null : FormatReceipt(rcpt.Text);
        }

        public string GetMerchantReversalReceipt()
        {
            if (Receipts.Count == 0) return null;

            Receipt rcpt = Receipts.FirstOrDefault(r => r.IsReversalReceipt && r.Flags.IsFlagSet("MC"));
            return rcpt == null ? null : FormatReceipt(rcpt.Text);
        }

        public string GetCustumerReversalReceipt()
        {
            if (Receipts.Count == 0) return null;

            Receipt rcpt = Receipts.FirstOrDefault(r => r.IsReversalReceipt && !r.Flags.IsFlagSet("MC"));
            return rcpt == null ? null : FormatReceipt(rcpt.Text);
        }

        private static string FormatReceipt(string text)
        {
            return text;
        }

        public IDisposable CreateAutoSaveContext()
        {
            if (!_autoSaveContext.WaitOne(300000))
                throw new Exception("Cannot create auto-save context");

            return _autoSaver = new AutoSaver(Save, _autoSaveContext);
        }

        private void AutoSave()
        {
            if (_autoSaver == null || !_autoSaver.Active)
                return;

            Save();
        }

        private void Save()
        {
            //throw new NotImplementedException();
        }

        public static TransactionState Load(byte[] data, Action<byte[]> onSave)
        {
            throw new NotImplementedException();
        }

        private class AutoSaver : IDisposable
        {
            public bool Active { get; private set; }

            private readonly Action _onSave;
            private readonly Semaphore _accessControl;

            public AutoSaver(Action onSave, Semaphore accessControl)
            {
                Ensure.NotNull(onSave, "onSave");
                Ensure.NotNull(accessControl, "accessControl");

                _onSave = onSave;
                _accessControl = accessControl;
                Active = true;
            }

            public void Dispose()
            {
                try
                {
                    _onSave();
                }
                finally
                {
                    _accessControl.Release();
                    Active = false;
                }
            }
        }
    }
}
