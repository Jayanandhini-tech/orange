using System.ComponentModel.DataAnnotations;

namespace VM.Domains;
public class FaceDeviceSetting
{
    [Key]
    [MaxLength(35)]
    public required string MachineId { get; set; }

    [MaxLength(20)]
    public string IpAddress { get; set; } = string.Empty;
}
