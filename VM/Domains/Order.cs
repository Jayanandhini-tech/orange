using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace VM.Domains;

public class Order
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [MaxLength(15)]
    public string OrderNumber { get; set; } = string.Empty;

    [Precision(0)]
    public DateTime OrderDate { get; set; }

    public string DeliveryType { get; set; } = string.Empty;

    [Precision(2)]
    public double Total { get; set; }

    [MaxLength(15)]
    public string Status { get; set; } = string.Empty;

    public string PaymentType { get; set; } = string.Empty;

    public bool IsPaid { get; set; }

    public double PaidAmount { get; set; }

    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    public bool IsRefunded { get; set; }

    [Precision(2)]
    public double RefundedAmount { get; set; }

    [MaxLength(40)]
    public string PaymentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsViewed { get; set; } = false;

    public virtual List<OrderItem>? Items { get; set; }
}
