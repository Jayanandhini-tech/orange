using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class CashRefund : VendorEntity, IAppTypeEntity
{
    public int Id { get; set; }

    public string RefId { get; set; } = string.Empty;

    [MaxLength(15)]
    public string OrderNumber { get; set; } = string.Empty;

    [MaxLength(12)]
    public string MobileNumber { get; set; } = string.Empty;
    public int Amount { get; set; }

    [MaxLength(50)]
    public string Denomination { get; set; } = string.Empty;

    public bool IsPaid { get; set; }

    [Precision(0)]
    public DateTime CancelOn { get; set; }

    [Precision(0)]
    public DateTime? SettledOn { get; set; } = null;

    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;
}
