using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.API.Domains;

[Index(nameof(AppType))]
public class Order : VendorEntity, IAppTypeEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [MaxLength(15)]
    public string OrderNumber { get; set; } = string.Empty;

    [Precision(0)]
    public DateTime OrderDate { get; set; }

    [Precision(2)]
    public double Total { get; set; }

    [MaxLength(15)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(40)]
    public string MachineId { get; set; } = string.Empty;

    [MaxLength(15)]
    public string DeliveryType { get; set; } = string.Empty;

    [MaxLength(15)]
    public string PaymentType { get; set; } = string.Empty;

    public bool IsPaid { get; set; }

    [Precision(2)]
    public double PaidAmount { get; set; }

    public bool IsRefunded { get; set; }

    [Precision(2)]
    public double RefundedAmount { get; set; }

    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? PaymentId { get; set; }

    public string AppUserId { get; set; } = string.Empty;


    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;

    public virtual List<OrderItem>? Items { get; set; }


    [ForeignKey(nameof(PaymentId))]
    public virtual Payment? Payment { get; set; }
}
