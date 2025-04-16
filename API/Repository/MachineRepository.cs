using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class MachineRepository : Repository<Machine>, IMachineRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public MachineRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }


    public async Task<Machine?> GetMachineAsync(string machineId)
    {
        return await db.Machines.FindAsync(machineId);
    }

    public async Task<bool> MachineBelongsToVendor(string machineId)
    {
        return await db.Machines.AnyAsync(x => x.VendorId == tenant.VendorId && x.Id == machineId);
    }


    public async Task<List<string>> GetUserAppTypesAsync()
    {
        var appTypes = await db.Machines.Where(x => x.VendorId == tenant.VendorId).Select(x => x.AppType).Distinct().ToListAsync();
        return appTypes ?? [];
    }

    public void Update(Machine machine)
    {
        db.Update(machine);
    }
}
