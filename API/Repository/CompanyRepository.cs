using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class CompanyRepository : Repository<Company>, ICompanyRepository
{
    public CompanyRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}

