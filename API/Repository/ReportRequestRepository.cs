using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class ReportRequestRepository : Repository<ReportRequest>, IReportRequestRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public ReportRequestRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    public async Task<ReportRequest?> GetRequestByIdAsync(string Id)
    {
        return await db.ReportRequests.Where(x => x.VendorId == tenant.VendorId && x.Id == Id).FirstOrDefaultAsync();
    }

    public async Task<List<ReportRequest>> GetAllMachineRequestsAsync(string MachineId)
    {
        return await db.ReportRequests.Where(x => x.VendorId == tenant.VendorId && x.MachineId == MachineId).ToListAsync();
    }
}
