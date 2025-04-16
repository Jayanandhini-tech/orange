using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class CashRefundRepository : Repository<CashRefund>, ICashRefundRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public CashRefundRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }


    public async Task<List<CashRefundsDto>> GetCashRefund(DateTime fromDate, DateTime toDate)
    {
        var records = await db.CashRefunds
                                .Where(x => x.VendorId == tenant.VendorId &&
                                            x.CancelOn >= fromDate &&
                                            x.CancelOn <= toDate)
                                .Select(x => new CashRefundsDto(x.CancelOn, x.OrderNumber, x.MobileNumber, x.Amount, x.Denomination, x.IsPaid))
                                .ToListAsync();

        return records.OrderBy(x => x.Date).ToList() ?? [];
    }


    public async Task MarkAsPaid(string orderNumber, string mobileNumber)
    {
        var record = await db.CashRefunds.FirstOrDefaultAsync(x => x.VendorId == tenant.VendorId && x.OrderNumber == orderNumber && x.MobileNumber == mobileNumber);
        if (record is null)
            return;

        record.IsPaid = true;
        record.SettledOn = DateTime.Now;

        await db.SaveChangesAsync();
    }
}
