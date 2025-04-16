namespace VM.Dtos;

public class OrderPaymentDto
{
    public string PaymentType { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public double Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}
