using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class AppUserRepository : Repository<AppUser>, IAppUserRepository
{
    public AppUserRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
