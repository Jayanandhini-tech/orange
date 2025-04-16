using System.ComponentModel.DataAnnotations;

namespace VM.Domains;

public class Machine
{
    [Key]
    public required string Id { get; set; }
    public int MachineNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VendorShortName { get; set; } = string.Empty;
    public string AppType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
