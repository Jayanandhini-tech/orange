namespace CMS.Dto;

public class CashRefundDto
{
    public string RefId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Denomination { get; set; } = string.Empty;
    public DateTime CancelOn { get; set; }
}

public record CashRefundsDto(DateTime Date, string OrderNumber, string MobileNumber, double Amount, string Denomination, bool IsPaid);