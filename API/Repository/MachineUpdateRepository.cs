using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class MachineUpdateRepository : Repository<MachineUpdateInfo>, IMachineUpdateRepository
{
    private readonly AppDBContext db;


    public MachineUpdateRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
    }
}
