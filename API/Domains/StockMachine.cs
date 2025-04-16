using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.API.Domains;

public class StockMachine : VendorEntity, IAppTypeEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    public string MachineId { get; set; } = string.Empty;

    public int CabinId { get; set; }

    public int MotorNumber { get; set; }

    public string? ProductId { get; set; }

    public int Capacity { get; set; }

    public int Stock { get; set; }

    public bool Soldout { get; set; }

    [Precision(0)]
    public DateTime UpdatedOn { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
