namespace CMS.API.Payments.Dtos;

public class UPICreateResponse
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string UpiString { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
