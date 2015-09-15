namespace PlugNPayHub.Device.PedControl.Eps.Messages
{
    enum AsyncPosEventTypes
    {
        Ack,
        Nak,
        CardReader,
        CardRemoved,
        DocClosed,
        AuthorizationStarted,
        AuthorizationResult,
        DisplayText,
        GetPromptInput,
        PrintReceipt,
        AdjustResult
    }
}
