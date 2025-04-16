using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class VendorRepository : Repository<Vendor>, IVendorRepository
{
    private readonly AppDBContext db;

    public VendorRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
    }

    public void Update(Vendor vendor)
    {
        db.Update(vendor);
    }
}
