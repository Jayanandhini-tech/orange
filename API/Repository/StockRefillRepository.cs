using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class StockRefillRepository : Repository<StockRefill>, IStockRefillRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public StockRefillRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    public async Task<List<RefillItemDto>> GetRefillRecords(string machineId, DateTime fromDate, DateTime toDate)
    {
        var query = db.StockRefills
                        .Where(x => x.VendorId == tenant.VendorId && x.RefilledOn >= fromDate && x.RefilledOn <= toDate);

        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);

        var refills = await query.Join(
                                    db.Products,
                                    r => r.ProductId,
                                    p => p.Id,
                                    (r, p) => new RefillItemDto
                                    (
                                        r.RefilledOn,
                                        r.MotorNumber,
                                        p.Name,
                                        p.Price,
                                        r.Quantity,
                                         p.Price * r.Quantity
                                    ))
                                    .AsSplitQuery()
                                    .ToListAsync();

        return refills.OrderBy(x => x.Date).ToList() ?? [];
    }

    public async Task<List<RefillSummaryDto>> GetRefillSummary(string machineId, DateTime fromDate, DateTime toDate)
    {
        var query = db.StockRefills.Where(x => x.VendorId == tenant.VendorId && x.RefilledOn >= fromDate && x.RefilledOn <= toDate);
        if (!string.IsNullOrEmpty(machineId))
            query = query.Where(x => x.MachineId == machineId);
        var refills = await query.OrderBy(x => x.RefilledOn).ToListAsync();


        var query1 = db.StockCleared.Where(x => x.VendorId == tenant.VendorId && x.ClearedOn >= fromDate && x.ClearedOn <= toDate);
        if (!string.IsNullOrEmpty(machineId))
            query1 = query1.Where(x => x.MachineId == machineId);
        var cleared = await query1.OrderBy(x => x.ClearedOn).ToListAsync();



        List<StockTrans> stockTrans = [.. refills.Select(x => new StockTrans(x.RefilledOn, x.ProductId, x.Quantity, 0)).ToList(),
                                        ..cleared.Select(x => new StockTrans(x.ClearedOn, x.ProductId, 0 , x.Quantity)).ToList()];


        if (stockTrans.Count == 0)
            return new List<RefillSummaryDto>();


        var prodIds = stockTrans.Select(stockTrans => stockTrans.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => prodIds.Contains(x.Id)).Select(x => new { x.Id, x.Name, x.Price }).ToListAsync();

        stockTrans = stockTrans.OrderBy(x => x.Date).ToList();
        List<Slot> slots = new List<Slot>();



        DateTime begin = stockTrans[0].Date;
        DateTime lastRefill = begin;

        for (int i = 1; i < stockTrans.Count; i++)
        {
            var diff = stockTrans[i].Date - lastRefill;
            if (diff.TotalMinutes > 30)
            {
                slots.Add(new Slot(slots.Count + 1, begin, lastRefill));
                begin = stockTrans[i].Date;
            }
            lastRefill = stockTrans[i].Date;
        }

        slots.Add(new Slot(slots.Count + 1, begin, lastRefill));



        var ss = stockTrans.Join(products, s => s.ProductId, p => p.Id, (s, p) => new { s.Date, s.ProductId, p.Name, p.Price, s.Refilled, s.Cleared }).ToList();

        List<RefillSummaryDto> summaries = new List<RefillSummaryDto>();
        foreach (var slot in slots)
        {
            var slotRecords = ss.Where(x => x.Date >= slot.Start && x.Date <= slot.End)
                                        .GroupBy(x => x.ProductId)
                                        .Select(g => new { g.First().Name, g.First().Price, Filled = g.Sum(x => x.Refilled), Cleared = g.Sum(x => x.Cleared) })
                                        .Select(s => new RefillSummaryItemDto(s.Name, s.Price, s.Filled, s.Cleared, s.Filled - s.Cleared, (s.Filled - s.Cleared) * s.Price))
                                        .ToList();
            summaries.Add(new RefillSummaryDto(slot.id, slot.Start, slot.End, slotRecords));
        }

        return summaries;
    }



    private record Slot(int id, DateTime Start, DateTime End);
    private record StockTrans(DateTime Date, string ProductId, int Refilled, int Cleared);

}
