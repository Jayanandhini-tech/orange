using CMS.Dto.Payments;

namespace CMS.API.Payments.Dtos;

public class UPIStatusResponse
{
    public bool Success { get; set; }
    public PaymentStatusCode Status { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string BankRRR { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
