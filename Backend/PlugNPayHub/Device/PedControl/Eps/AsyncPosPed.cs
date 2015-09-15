using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PlugNPayHub.Device.PedControl.Eps.Messages;
using PlugNPayHub.PosControl.Messages;
using PlugNPayHub.Utils;

namespace PlugNPayHub.Device.PedControl.Eps
{
    class AsyncPosPed : IPed
    {
        private const int AckTimeout = 5000;
        private const int AuthorizationTimeout = 60000;
        private const int CardWaitTimeout = 120000;
        private const int CardExpireTimeout = 600000;
        private const int ReversalReceiptTimeout = 10000;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public string PedId { get; }

        private readonly IPedHub _pedHub;
        private readonly byte[] _apak;

        private static readonly Languages Language = new Languages();
        private string _languageCode = "EN";

        private readonly EventsMonitor<Format0Reader> _pedEvents = new EventsMonitor<Format0Reader>();
        private readonly Dictionary<string, TransactionState> _transactions = new Dictionary<string, TransactionState>();

        public AsyncPosPed(string pedId, byte[] apak, IPedHub hub)
        {
            Ensure.NotNull(pedId, nameof(pedId));
            Ensure.NotNull(hub, nameof(hub));
            Ensure.NotNull(apak, nameof(apak));

            PedId = pedId;
            _pedHub = hub;
            _apak = apak;
        }

        public async Task<ITransactionResponse> AuthorizeAsync(AuthorizeRequest authorizeRequest)
        {
            #region Validate request

            if (string.IsNullOrEmpty(authorizeRequest.Language)) throw new AsyncPosException(Language.Get("language_error", _languageCode));
            _languageCode = authorizeRequest.Language;

            if (authorizeRequest.Amount == 0) throw new AsyncPosException(Language.Get("amount_error", _languageCode));
            if (authorizeRequest.Cash < 0) throw new AsyncPosException(Language.Get("cash_error", _languageCode));
            if (authorizeRequest.Currency == 0) throw new AsyncPosException(Language.Get("currency_error", _languageCode));

            #endregion

            TransactionState transactionState = GetTransactionState(authorizeRequest.TransactionId, true);
            transactionState.Language = _languageCode;

            if (transactionState.State != TransactionStates.NotStarted)
                throw new AsyncPosException(Language.Get("docnum_error", transactionState.Language));

            using (transactionState.CreateAutoSaveContext())
            {
                try
                {
                    transactionState.Amount = authorizeRequest.Amount;
                    transactionState.Cash = authorizeRequest.Cash;
                    transactionState.Currency = authorizeRequest.Currency;
                    transactionState.Last4CardNumberDigits = authorizeRequest.Last4CardNumberDigits;
                    transactionState.PreAuthorize = authorizeRequest.PreAuthorize;

                    await SendLoginRequest(transactionState.Language, Language.Get("pinpad_welcome", transactionState.Language));
                    await GetCardToken(transactionState);
                    await ProcessAuthorization(transactionState);
                }
                catch (AsyncPosException ex)
                {
                    transactionState.InformationText = ex.Message;
                }
            }

            AuthorizeResponse response = new AuthorizeResponse
            {
                TransactionId = authorizeRequest.TransactionId,
                Result = transactionState.State == TransactionStates.Approved ? ResponseResults.Ok : ResponseResults.Error,
                Amount = transactionState.Amount,
                Text = (transactionState.State == TransactionStates.Approved) ? transactionState.InformationText : (transactionState.InformationText ?? "System error"),
                AuthorizationCode = transactionState.AuthorizationCode,
                Rrn = transactionState.Rrn,
                Stan = transactionState.Stan,
                CardType = transactionState.CardType
            };

            FillReceipts(transactionState, response, false);

            return response;
        }

        public async Task<ITransactionResponse> ConfirmAsync(ConfirmRequest confirmRequest)
        {
            Ensure.NotNull(confirmRequest.TransactionId, nameof(confirmRequest.TransactionId));

            TransactionState transactionState = GetTransactionState(confirmRequest.TransactionId, false);
            if (transactionState == null)
                throw new AsyncPosException(string.Concat(Language.Get("no_trans_error", _languageCode), $" {nameof(confirmRequest.TransactionId)}: ", confirmRequest.TransactionId));

            switch (transactionState.State)
            {
                case TransactionStates.Approved:
                case TransactionStates.Confirming:
                    {
                        if (string.IsNullOrEmpty(transactionState.AuthorizationId))
                            throw new AsyncPosException(Language.Get("cannot_confirm_trans_error", transactionState.Language));

                        if (transactionState.PreAuthorize && transactionState.Amount != confirmRequest.Amount)
                        {
                            if (confirmRequest.Amount <= 0 || confirmRequest.Amount > transactionState.Amount)
                                throw new AsyncPosException(Language.Get("cannot_confirm_invalid_amount_trans_error", transactionState.Language));

                            await ProcessAdjust(transactionState, confirmRequest.Amount);
                        }

                        await ProcessConfirm(transactionState);
                        break;
                    }

                case TransactionStates.Confirmed:
                    if (transactionState.PreAuthorize && transactionState.Amount != confirmRequest.Amount)
                        throw new AsyncPosException(Language.Get("cannot_confirm_invalid_amount_trans_error", transactionState.Language));
                    break;

                default:
                    throw new AsyncPosException(Language.Get("cannot_confirm_trans_error", transactionState.Language));
            }

            if (!transactionState.PreAuthorize)
                return new ConfirmResponse { TransactionId = confirmRequest.TransactionId, Result = ResponseResults.Ok, Text = transactionState.InformationText };

            ConfirmAdjustResponse response = new ConfirmAdjustResponse { TransactionId = confirmRequest.TransactionId, Result = ResponseResults.Ok, Text = transactionState.InformationText };
            FillReceipts(transactionState, response, false);

            return response;
        }

        public async Task<ITransactionResponse> ReversalAsync(ReversalRequest reversalRequest)
        {
            Ensure.NotNull(reversalRequest.TransactionId, nameof(reversalRequest.TransactionId));

            TransactionState transactionState = GetTransactionState(reversalRequest.TransactionId, false);
            if (transactionState == null)
                throw new AsyncPosException(string.Concat(Language.Get("no_trans_error", _languageCode), $" {nameof(reversalRequest.TransactionId)}: ", reversalRequest.TransactionId));

            #region Fire event

            Format0Writer writer = new Format0Writer();
            writer.WriteStart("DocClosed");
            writer.WriteData("DocNr", reversalRequest.TransactionId);
            writer.WriteEnd();

            _pedEvents.FireEvent(AsyncPosEventTypes.DocClosed.ToString(), new Format0Reader(writer.ToArray()));

            #endregion

            switch (transactionState.State)
            {
                case TransactionStates.Reversed:
                case TransactionStates.NotStarted:
                case TransactionStates.Declined:
                    transactionState.State = TransactionStates.Reversed;
                    break;

                case TransactionStates.Confirming:
                case TransactionStates.Confirmed:
                    throw new AsyncPosException(Language.Get("cannot_reverse_trans_error", transactionState.Language));

                default:
                    {
                        await ProcessReversal(transactionState);
                        break;
                    }
            }

            ReversalResponse response = new ReversalResponse
            {
                TransactionId = reversalRequest.TransactionId,
                Result = transactionState.State == TransactionStates.Reversed ? ResponseResults.Ok : ResponseResults.Error,
                Text = transactionState.InformationText,
            };

            FillReceipts(transactionState, response, true);

            return response;
        }

        public void Process(byte[] receivedData)
        {
            Ensure.NotNull(receivedData, nameof(receivedData));

            byte messageType = AsyncPosPacket.GetType(receivedData);
            switch (messageType)
            {
                case 0x00:
                case 0x01:
                    {
                        string packetId = AsyncPosPacket.GetId(receivedData);
                        Log.Info("Received message from Ped, length {0} bytes, packet type 0x{1}, Id {2} {3} {4}", receivedData.Length, messageType.ToString("X2"), packetId, Environment.NewLine, AsyncPosPacket.DumpF0(AsyncPosPacket.GetMsg(receivedData)));

                        string posId = AsyncPosPacket.GetPosId(receivedData);
                        if (PedId != posId)
                        {
                            Log.Error("Received posid '{1}' does not match expected '{0}'", PedId, posId);
                            return;
                        }

                        if (!AsyncPosPacket.CheckSignature(receivedData, 0, _apak))
                        {
                            Log.Error("APAK does not match");
                            _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(AsyncPosPacket.BuildNak(receivedData)));
                            return;
                        }

                        Format0Reader receivedMessageReader = new Format0Reader(AsyncPosPacket.GetMsg(receivedData));
                        _pedEvents.FireEvent(receivedMessageReader.RootTag, receivedMessageReader);

                        _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(AsyncPosPacket.BuildAck(receivedData)));

                        switch (receivedMessageReader.RootTag)
                        {
                            case "printreceipt":
                                {
                                    PrintReceipt printReceipt = PrintReceipt.Deserialize(new Format0Reader(AsyncPosPacket.GetMsg(receivedData)));
                                    SendPrintResult(printReceipt.ReceiptId, printReceipt.DocumentNumber);
                                    break;
                                }
                        }
                        break;
                    }

                case 0x0A:
                    {
                        string packetId = AsyncPosPacket.GetId(receivedData);
                        Log.Info("ACK from PED, Id {0}", packetId);

                        _pedEvents.FireEvent(AsyncPosEventTypes.Ack.ToString(), null);
                        break;
                    }
                case 0x0F:
                    {
                        string packetId = AsyncPosPacket.GetId(receivedData);
                        Log.Info("NAK from PED, Id {0}", packetId);

                        _pedEvents.FireEvent(AsyncPosEventTypes.Nak.ToString(), null);
                        break;
                    }

                default:
                    Log.Warn("Ignoring packet 0x{0}", messageType.ToString("X2"));
                    break;
            }
        }

        #region Processing help functions

        private void SendPrintResult(string receiptId, string documentNumber)
        {
            PrintResult printResult = PrintResult.Create(receiptId, documentNumber);
            _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(printResult.Serialize(_apak, PedId)));
        }

        private async Task SendLoginRequest(string language, string idleText)
        {
            LoginRequest login = LoginRequest.Create(language, idleText);
            _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(login.Serialize(_apak, PedId)));

            if (await _pedEvents.WaitOneAsync(AsyncPosEventTypes.Ack.ToString(), AckTimeout) == null)
                throw new AsyncPosException(Language.Get("no_response", _languageCode));
        }

        private async Task GetCardToken(TransactionState transactionState)
        {
            string cardToken = await GetOldCardToken(transactionState.Time);
            if (!string.IsNullOrEmpty(cardToken))
            {
                transactionState.CardToken = cardToken;
                return;
            }

            DisplayText displayText = DisplayText.Create(Language.Get("insert_card", transactionState.Language), DisplayText.DisplayTextReasons.CardWaiting, DisplayText.BeepTypes.SingleLong);
            _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(displayText.Serialize(_apak, PedId)));

            var result = await _pedEvents.WaitAnyAsync(new[]
            {
                AsyncPosEventTypes.DocClosed.ToString(),
                AsyncPosEventTypes.CardReader.ToString()
            }, CardWaitTimeout);

            if (result == null)
                throw new AsyncPosException(Language.Get("card_timeout", transactionState.Language));

            if (result.Id == AsyncPosEventTypes.DocClosed.ToString())
                throw new AsyncPosException(Language.Get("cancelled", transactionState.Language));

            CardReader cardReader = CardReader.Deserialize(result.Value);
            if (string.IsNullOrEmpty(cardReader.Token))
                throw new AsyncPosException(Language.Get("card_timeout", transactionState.Language));

            transactionState.CardToken = cardReader.Token;
        }

        private async Task<string> GetOldCardToken(DateTime operationStart)
        {
            var cardReaderEvent = await _pedEvents.WaitOneAsync(AsyncPosEventTypes.CardReader.ToString(), 0);

            //Check if CardReader is not too old
            if (cardReaderEvent == null || cardReaderEvent.FiredTime.AddMilliseconds(CardExpireTimeout) < operationStart)
                return null;

            CardReader cardReader = CardReader.Deserialize(cardReaderEvent.Value);
            if (string.IsNullOrEmpty(cardReader.Token)) return null;

            switch (cardReader.CardReadMethod)
            {
                case CardReader.CardReadMethods.Chip:
                case CardReader.CardReadMethods.Nfc:
                    break;

                default:
                    return null;
            }

            //Check is card inserted
            var cardRemovedEvent = await _pedEvents.WaitOneAsync(AsyncPosEventTypes.CardRemoved.ToString(), 0);
            if (cardRemovedEvent != null && cardRemovedEvent.FiredTime > cardReaderEvent.FiredTime)
            {
                CardRemoved cardRemoved = CardRemoved.Deserialize(cardRemovedEvent.Value);
                if (cardRemoved.Token == cardReader.Token)
                    return null;
            }

            return cardReader.Token;
        }

        private async Task ProcessAuthorization(TransactionState transactionState)
        {
            transactionState.State = TransactionStates.Authorizing;
            AuthorizationRequest authRequest = AuthorizationRequest.Create(transactionState.DocumentNumber, transactionState.Amount, transactionState.Cash, transactionState.CardToken, transactionState.Time, transactionState.Currency, transactionState.Language, transactionState.PreAuthorize);

            _pedEvents.Clear();

            _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(authRequest.Serialize(_apak, PedId)));

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (sw.Elapsed.TotalMilliseconds < AuthorizationTimeout)
            {
                var eventData = await _pedEvents.WaitAnyAsync(new[]
                {
                    AsyncPosEventTypes.DocClosed.ToString(),
                    AsyncPosEventTypes.AuthorizationStarted.ToString(),
                    AsyncPosEventTypes.AuthorizationResult.ToString(),
                    AsyncPosEventTypes.DisplayText.ToString(),
                    AsyncPosEventTypes.GetPromptInput.ToString(),
                    AsyncPosEventTypes.PrintReceipt.ToString(),
                }, AuthorizationTimeout - (int)sw.Elapsed.TotalMilliseconds);

                if (eventData == null) continue;

                switch ((AsyncPosEventTypes)Enum.Parse(typeof(AsyncPosEventTypes), eventData.Id, true))
                {
                    case AsyncPosEventTypes.GetPromptInput:
                        {
                            GetPromptInput prompt = GetPromptInput.Deserialize(eventData.Value);

                            if (prompt.Rerason != GetPromptInput.PromptReasons.Last4Digits)
                                throw new NotSupportedException("Not supported GetPromptInput request");

                            await SendLast4Digits(transactionState.DocumentNumber, transactionState.Last4CardNumberDigits);

                            sw.Reset();
                            sw.Start();
                            break;
                        }
                    case AsyncPosEventTypes.DisplayText:
                        {
                            DisplayText displayText = DisplayText.Deserialize(eventData.Value);
                            if (displayText.Reason == DisplayText.DisplayTextReasons.TimeoutWaiting)
                            {
                                sw.Reset();
                                sw.Start();
                            }
                            break;
                        }
                    case AsyncPosEventTypes.AuthorizationResult:
                        {
                            AuthorizationResponse result = AuthorizationResponse.Deserialize(eventData.Value);
                            if (result.DocumentNumber != transactionState.DocumentNumber) break;

                            transactionState.State = result.Approved ? TransactionStates.Approved : TransactionStates.Declined;
                            transactionState.InformationText = result.Text;
                            transactionState.Rrn = result.Rrn;
                            transactionState.Stan = result.Stan;
                            transactionState.Amount = result.Amount < transactionState.Amount ? result.Amount : transactionState.Amount;
                            transactionState.AuthorizationCode = result.AuthorizationCode;
                            transactionState.AuthorizationId = result.AuthorizationId;
                            transactionState.CardType = result.CardType;
                            return;
                        }
                    case AsyncPosEventTypes.PrintReceipt:
                        {
                            PrintReceipt printReceipt = PrintReceipt.Deserialize(eventData.Value);
                            if (printReceipt.DocumentNumber != transactionState.DocumentNumber) break;

                            transactionState.Receipts.Add(new Receipt
                            {
                                Id = printReceipt.ReceiptId,
                                Text = printReceipt.ReceiptText,
                                Flags = printReceipt.Flags
                            });
                            break;
                        }

                    case AsyncPosEventTypes.DocClosed:
                        {
                            DocClosedRequest docClosed = DocClosedRequest.Deserialize(eventData.Value);
                            if (docClosed.DocumentNumber != transactionState.DocumentNumber) break;

                            throw new AsyncPosException(Language.Get("cancelled", transactionState.Language));
                        }
                }
            }

            throw new AsyncPosException(Language.Get("no_response", transactionState.Language));
        }

        private async Task ProcessConfirm(TransactionState transactionState)
        {
            using (transactionState.CreateAutoSaveContext())
            {
                transactionState.State = TransactionStates.Confirming;
                DocClosedRequest docClosed = DocClosedRequest.Create(transactionState.DocumentNumber, transactionState.AuthorizationId);

                _pedEvents.Clear();
                _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(docClosed.Serialize(_apak, PedId)));

                var result = await _pedEvents.WaitOneAsync(AsyncPosEventTypes.Ack.ToString(), AckTimeout);
                if (result == null)
                    throw new AsyncPosException(Language.Get("no_response", transactionState.Language));

                transactionState.State = TransactionStates.Confirmed;
            }
        }

        private async Task ProcessAdjust(TransactionState transactionState, long newAmount)
        {
            using (transactionState.CreateAutoSaveContext())
            {
                AdjustRequest adjustRequest = AdjustRequest.Create(transactionState.DocumentNumber, newAmount, transactionState.AuthorizationId);

                _pedEvents.Clear();
                _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(adjustRequest.Serialize(_apak, PedId)));

                transactionState.Receipts.Clear();

                Stopwatch sw = new Stopwatch();
                sw.Start();

                while (sw.Elapsed.TotalMilliseconds < AuthorizationTimeout)
                {
                    var eventData = await _pedEvents.WaitAnyAsync(new[]
                    {
                        AsyncPosEventTypes.AdjustResult.ToString(),
                        AsyncPosEventTypes.PrintReceipt.ToString(),
                    }, AuthorizationTimeout - (int)sw.Elapsed.TotalMilliseconds);

                    if (eventData == null) continue;

                    switch ((AsyncPosEventTypes)Enum.Parse(typeof(AsyncPosEventTypes), eventData.Id, true))
                    {
                        case AsyncPosEventTypes.AdjustResult:
                            {
                                AdjustResponse result = AdjustResponse.Deserialize(eventData.Value);
                                if (result.DocumentNumber != transactionState.DocumentNumber) break;

                                if (!result.Approved)
                                    throw new AsyncPosException(Language.Get("cannot_confirm_partial_trans_error", transactionState.Language));

                                transactionState.Amount = newAmount;
                                return;
                            }
                        case AsyncPosEventTypes.PrintReceipt:
                            {
                                PrintReceipt printReceipt = PrintReceipt.Deserialize(eventData.Value);
                                if (printReceipt.DocumentNumber != transactionState.DocumentNumber) break;

                                transactionState.Receipts.Add(new Receipt
                                {
                                    Id = printReceipt.ReceiptId,
                                    Text = printReceipt.ReceiptText,
                                    Flags = printReceipt.Flags
                                });

                                break;
                            }
                    }
                }

                throw new AsyncPosException(Language.Get("no_response", transactionState.Language));
            }
        }

        private async Task ProcessReversal(TransactionState transactionState)
        {
            using (transactionState.CreateAutoSaveContext())
            {
                transactionState.State = TransactionStates.Reversing;
                transactionState.InformationText = Language.Get("reversed", transactionState.Language);

                _pedEvents.Clear();

                DocClosedRequest docClosed = DocClosedRequest.Create(transactionState.DocumentNumber, null);
                _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(docClosed.Serialize(_apak, PedId)));

                if (await _pedEvents.WaitOneAsync(AsyncPosEventTypes.Ack.ToString(), AckTimeout) == null)
                    throw new AsyncPosException(Language.Get("no_response", transactionState.Language));

                transactionState.State = TransactionStates.Reversed;

                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.Elapsed.TotalMilliseconds < ReversalReceiptTimeout)
                {

                    var eventData = await _pedEvents.WaitOneAsync(AsyncPosEventTypes.PrintReceipt.ToString(), ReversalReceiptTimeout - (int)sw.Elapsed.TotalSeconds);
                    if (eventData == null) continue;

                    PrintReceipt printReceipt = PrintReceipt.Deserialize(eventData.Value);
                    if (printReceipt.DocumentNumber != transactionState.DocumentNumber) continue;

                    Receipt rcpt = new Receipt
                    {
                        Id = printReceipt.ReceiptId,
                        Text = printReceipt.ReceiptText,
                        Flags = printReceipt.Flags,
                        IsReversalReceipt = true,
                    };

                    transactionState.Receipts.Add(rcpt);

                    if (printReceipt.Flags.IsFlagSet("LR")) break;
                }
            }
        }

        private async Task SendLast4Digits(string documentNumber, string last4Digits)
        {
            InputResult inpurResult = InputResult.Create(documentNumber, last4Digits, InputResult.Source.Prompt);
            _pedHub.SendData(PedId, AsyncPosPacket.PrependLength(inpurResult.Serialize(_apak, PedId)));

            var result = await _pedEvents.WaitOneAsync(AsyncPosEventTypes.Ack.ToString(), AckTimeout);
            if (result == null)
                throw new AsyncPosException("Timeout on InputResult");

            if (string.IsNullOrEmpty(last4Digits))
                throw new AsyncPosException("Missing last 4 digits in request message");
        }

        private static void FillReceipts(TransactionState transactionState, IReceiptResponse response, bool reversalReceipts)
        {
            try
            {
                StringBuilder clientReceipt = new StringBuilder();
                StringBuilder merchantReceipt = new StringBuilder();

                foreach (Receipt receipt in transactionState.Receipts.Where(r => r.IsReversalReceipt == reversalReceipts))
                {
                    if (receipt.Flags.IsFlagSet("SGN"))
                        response.NeedClientSignature = true;

                    if (receipt.Flags.IsFlagSet("MC"))
                        merchantReceipt.Append(receipt.Text);
                    else
                        clientReceipt.Append(receipt.Text);
                }

                response.ClientReceipt = clientReceipt.ToString();
                response.MerchantReceipt = merchantReceipt.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        #endregion

        #region Transaction state handling

        private TransactionState GetTransactionState(string documentNumber, bool createNewIfNotExists)
        {
            lock (_transactions)
            {
                #region Try to get exitsing

                TransactionState state;
                if (!string.IsNullOrEmpty(documentNumber))
                {
                    if (_transactions.TryGetValue(documentNumber, out state)) return state;
                }

                #endregion

                if (!createNewIfNotExists)
                    return null;

                #region Create new

                DateTime time = DateTime.Now;
                documentNumber = string.IsNullOrEmpty(documentNumber) ? string.Concat("Num", time.ToString("yyyyMMddHHmmss")) : documentNumber;

                using ((state = new TransactionState()).CreateAutoSaveContext())
                {
                    state.Time = time;
                    state.Language = _languageCode;
                    state.DocumentNumber = documentNumber;
                }

                _transactions[state.DocumentNumber] = state;

                return state;

                #endregion
            }
        }

        #endregion

    }
}
