namespace CMS.API.Payments.Dtos.ICICI;

public class IciciQRDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RefId { get; set; } = string.Empty;
    public string QRbase64png { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
