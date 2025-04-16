using CMS.API.Domains;

namespace CMS.API.Repository.IRepository;

public interface IVendorRepository : IRepository<Vendor>
{
    public void Update(Vendor  vendor);
}
