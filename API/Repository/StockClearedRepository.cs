using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class StockClearedRepository : Repository<StockCleared>, IStockClearedRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public StockClearedRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    public async Task<List<RefillItemDto>> GetClearedRecords(string machineId, DateTime fromDate, DateTime toDate)
    {
        var query = db.StockCleared
                       .Where(x => x.VendorId == tenant.VendorId && x.ClearedOn >= fromDate && x.ClearedOn <= toDate);

        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);

        var cleared = await query.Join(
                                    db.Products,
                                    r => r.ProductId,
                                    p => p.Id,
                                    (r, p) => new RefillItemDto
                                    (
                                        r.ClearedOn,
                                        r.MotorNumber,
                                        p.Name,
                                        p.Price,
                                        r.Quantity,
                                         p.Price * r.Quantity
                                    ))
                                    .AsSplitQuery()
                                    .ToListAsync();
        return cleared.OrderBy(x => x.Date).ToList() ?? [];
    }
}
