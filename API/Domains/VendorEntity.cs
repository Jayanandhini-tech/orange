namespace CMS.API.Domains;

public interface IVendorEntity
{
    int VendorId { get; set; }
}

public abstract class VendorEntity : IVendorEntity
{
    public int VendorId { get; set; }
}
