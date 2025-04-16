namespace CMS.Dto;

public class MachineInfoDto
{
    public string Id { get; set; } = string.Empty;
    public int MachineNumber { get; set; }
    public string AppType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string VendorShortName { get; set; } = string.Empty;
}
