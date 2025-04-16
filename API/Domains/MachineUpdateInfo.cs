using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class MachineUpdateInfo : VendorEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    [Precision(0)]
    public DateTime LastUpdatedOn { get; set; }
}
