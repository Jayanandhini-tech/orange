using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class LogRequest : VendorEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [Precision(0)]
    public DateTime RequestedOn { get; set; }

    [MaxLength(40)]
    public string MachineId { get; set; } = string.Empty;

    [Precision(0)]
    public DateTime LogDate { get; set; }

    public bool Sent { get; set; } = false;

    public bool Received { get; set; } = false;

    public bool Success { get; set; } = false;

    public string Message { get; set; } = string.Empty;

    [MaxLength(200)]
    public string FilePath { get; set; } = string.Empty;
}
