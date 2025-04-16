namespace VM.Services.Interfaces;

public interface IBillValidatorService
{
    event Action<Dictionary<int, int>>? OnAmountReceived;
    event Action<string>? OnMessageRaised;

    bool Close();
    bool DisableValidator();
    bool EnableValidator();
    bool InitializeAmountRequest(int requestAmount, int maxLimit = 0);
    bool IsOpen();
    bool Open();
}