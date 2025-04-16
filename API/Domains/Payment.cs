using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

[Index(nameof(OrderNumber))]
public class Payment : VendorEntity, IAppTypeEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }
    
    [MaxLength(35)]
    public string OrderNumber { get; set; } = string.Empty;

    public double Amount { get; set; }
    public bool IsPaid { get; set; }

    [MaxLength(10)]
    public string PaymentType { get; set; } = string.Empty;

    [MaxLength(10)]
    public string PaymentProvider { get; set; } = string.Empty;

    [MaxLength(15)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(40)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(20)]
    public string BankRRN { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Reference { get; set; } = string.Empty;

    [Precision(0)]
    public DateTime PaymentOn { get; set; }

    public bool IsRefunded { get; set; }

    [MaxLength(35)]
    public string RefundId { get; set; } = string.Empty;

    public double RefundAmount { get; set; }

    [MaxLength(30)]
    public string RefundTransactionId { get; set; } = string.Empty;

    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;

    [MaxLength(40)]
    public string MachineId { get; set; } = string.Empty;
}
