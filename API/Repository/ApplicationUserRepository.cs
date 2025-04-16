using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
{
    private readonly AppDBContext db;

    public ApplicationUserRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
    }

    public void Update(ApplicationUser applicationUser)
    {
         db.ApplicationUsers.Update(applicationUser);
    }
}
