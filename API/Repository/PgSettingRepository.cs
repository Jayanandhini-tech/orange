using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class PgSettingRepository : Repository<PgSetting>, IPgSettingRepository
{
    public PgSettingRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
