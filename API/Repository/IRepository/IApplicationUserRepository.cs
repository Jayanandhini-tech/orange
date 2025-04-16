using CMS.API.Domains;

namespace CMS.API.Repository.IRepository;

public interface IApplicationUserRepository : IRepository<ApplicationUser>
{
    public void Update(ApplicationUser applicationUser);
}
