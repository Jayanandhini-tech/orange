using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class Recharge : VendorEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [MaxLength(35)]
    public string MachineId { get; set; } = string.Empty;

    [MaxLength(35)]
    public string AppUserId { get; set; } = string.Empty;

    public double Amount { get; set; }
    public bool IsSuccess { get; set; }

    [MaxLength(10)]
    public string PaymentType { get; set; } = string.Empty;

    [MaxLength(10)]
    public string PaymentProvider { get; set; } = string.Empty;

    [MaxLength(15)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(30)]
    public string TransactionId { get; set; } = string.Empty;

    [Precision(0)]
    public DateTime RechargedOn { get; set; }

}
