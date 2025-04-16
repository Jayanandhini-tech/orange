using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class LogRequestRepository : Repository<LogRequest>, ILogRequestRepository
{
    public LogRequestRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
