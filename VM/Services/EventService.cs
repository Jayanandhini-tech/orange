using VM.Services.Interfaces;

namespace VM.Services;

public class EventService : IEventService
{

    public bool IsNetworkAvailable { get; private set; } = false;

    public event Action<string>? OnOrderCompleted;
    public event Action<int>? OnCashPaymentCanceled;
    public event Action<string>? OnUserCreated;
    public event Action<string>? OnNetworkMessage;
    public event Action? OnBackToOnline;


    public void RaiseOrderCompleteEvent(string OrderNumber)
    {
        OnOrderCompleted?.Invoke(OrderNumber);
    }


    public void RaiseCashPaymentCanceled(int Id)
    {

        OnCashPaymentCanceled?.Invoke(Id);
    }

    public void RaiseUserCreatedEvent(string UserId)
    {
        OnUserCreated?.Invoke(UserId);
    }

    public void RaiseNetworkStatusChanged(bool Online)
    {
        if (IsNetworkAvailable != Online)
        {
            IsNetworkAvailable = Online;
            if (IsNetworkAvailable)
            {
                OnNetworkMessage?.Invoke("Internet connected");
                OnBackToOnline?.Invoke();
            }
            else
                OnNetworkMessage?.Invoke("Unable to connect with Internet");
        }
    }

    public void RaiseNetworkMessageEvent(string message)
    {
        OnNetworkMessage?.Invoke(message);
    }


}
