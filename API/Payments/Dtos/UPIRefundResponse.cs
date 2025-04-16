namespace CMS.API.Payments.Dtos;

public class UPIRefundResponse
{
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RefundId { get; set; } = string.Empty;
    public string RefundRefId { get; set; } = string.Empty;
}
