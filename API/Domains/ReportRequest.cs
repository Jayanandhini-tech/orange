using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class ReportRequest : VendorEntity, IAppTypeEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [Precision(0)]
    public DateTime RequestedOn { get; set; }

    [MaxLength(40)]
    public string MachineId { get; set; } = string.Empty;

    [Precision(0)]
    public DateTime From { get; set; }

    [Precision(0)]
    public DateTime To { get; set; }

    public bool Received { get; set; } = false;

    [MaxLength(200)]
    public string FilePath { get; set; } = string.Empty;

    public bool Sent { get; set; } = false;

    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;
}
