namespace CMS.API.Domains;
public class Company : VendorEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string GstNumber { get; set; } = string.Empty;
}

