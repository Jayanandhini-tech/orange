namespace VM.Services.Interfaces;

public interface IEventService
{
    bool IsNetworkAvailable { get; }

    public event Action<string>? OnOrderCompleted;
    public event Action<int>? OnCashPaymentCanceled;
    public event Action<string>? OnUserCreated;
    public event Action<string>? OnNetworkMessage;
    public event Action? OnBackToOnline;

    void RaiseNetworkMessageEvent(string message);

    void RaiseOrderCompleteEvent(string OrderNumber);

    void RaiseCashPaymentCanceled(int Id);

    void RaiseUserCreatedEvent(string UserId);
    void RaiseNetworkStatusChanged(bool Online);
}
