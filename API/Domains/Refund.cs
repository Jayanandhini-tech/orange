using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class Refund : VendorEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [MaxLength(35)]
    public string PaymentId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public bool IsRefund { get; set; }

    [MaxLength(15)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(30)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Reference { get; set; } = string.Empty;
        
    [Precision(0)]
    public DateTime RefundedOn { get; set; }
}
