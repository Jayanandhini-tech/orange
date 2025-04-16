namespace CMS.Dto;

public record OrderIdDto(string OrderId);
public record OrderNumberDto(string OrderNumber);

public record OrderSyncResultDto(List<string> OrderNumbers);

public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public double Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public double PaidAmount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public bool IsRefunded { get; set; }
    public double RefundedAmount { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
{
    public required string Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public double Rate { get; set; }
    public int Gst { get; set; }
    public double Price { get; set; }
    public int Qty { get; set; }
    public int VendQty { get; set; }
    public DateTime UpdatedOn { get; set; }
}
