using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class StockRefill : VendorEntity, IAppTypeEntity
{
    public required string Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public DateTime RefilledOn { get; set; }
    public int MotorNumber { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }

    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;
}
