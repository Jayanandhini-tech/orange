namespace CMS.API.Payments.Dtos.ICICI;

public class RefundDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OriginalBankRRN { get; set; } = string.Empty;
}
