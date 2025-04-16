using System.ComponentModel.DataAnnotations;

namespace CMS.Dto;

public class OrderCreateMachineDto
{

    public string Id { get; set; } = string.Empty;

    [MaxLength(15)]
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    [MaxLength(15)]
    public string DeliveryType { get; set; } = string.Empty;

    public double Total { get; set; }

    [MaxLength(15)]
    public string PaymentType { get; set; } = string.Empty; 

    public virtual List<OrderCreateMachineItemDto>? Items { get; set; }
}

public class OrderCreateMachineItemDto
{
    public string Id { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public double Rate { get; set; }

    public int Gst { get; set; }

    public double Price { get; set; }

    public int Qty { get; set; }

    public int VendQty { get; set; }
}