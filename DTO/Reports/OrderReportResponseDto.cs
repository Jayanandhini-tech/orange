namespace CMS.Dto.Reports;

public class OrderReportResponseDto
{
    public List<OrderReportDto>? Orders { get; set; }
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}


public class OrderReportDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double Paid { get; set; }
    public double Refund { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<OrderItemReportDto>? OrderItems { get; set; }
}

public class OrderItemReportDto
{
    public string ProductName { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Quantity { get; set; }
    public double Amount { get; set; }
}


public class OrderDownloadDto
{

    public DateTime OrderDate { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public double Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string OrderBy { get; set; } = string.Empty;
    public string OrderItems { get; set; } = string.Empty;
}


public record SalesUsersDto(string UserId, string Name, double Amount, List<string> Items);