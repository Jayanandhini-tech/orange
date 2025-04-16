using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class FaceDeviceSetting : VendorEntity
{
    public int Id { get; set; }

    [MaxLength(35)]
    public required string MachineId { get; set; }

    [MaxLength(20)]
    public required string IpAddress { get; set; }
}
