using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.API.Domains;

public class Machine : VendorEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;

    public int MachineNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mac { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public int PgSettingId { get; set; } 

    [Precision(0)]
    public DateTime CreatedOn { get; set; } = DateTime.Now;

    [Precision(0)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedOn { get; set; }

    public bool IsActive { get; set; } = true;
}
