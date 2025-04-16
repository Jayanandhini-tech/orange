using CMS.API.Domains;
using CMS.API.Payments.Services;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class RechargeRepository : Repository<Recharge>, IRechargeRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public RechargeRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    public async Task<List<Recharge>> GetUserRecentRechargeAsync(string userId)
    {
        var recharges = await db.Recharges
                            .Where(r => r.VendorId == tenant.VendorId && r.IsSuccess == true && r.AppUserId == userId)
                            .OrderByDescending(x => x.RechargedOn)
                            .Take(10)
                            .ToListAsync();
        return recharges;
    }

    public async Task<TransactionRequesterDto?> UpdateRechargeStatus(string rechargeId, string status, string transactionId)
    {
        var recharge = await db.Recharges.FirstOrDefaultAsync(x => x.Id == rechargeId);
        if (recharge is null)
            return null;


        recharge.Status = status;

        if (!string.IsNullOrEmpty(transactionId))
            recharge.TransactionId = transactionId;

        if (status == "SUCCESS")
        {
            if (recharge.IsSuccess == false)
            {
                recharge.IsSuccess = true;
                var appUser = await db.AppUsers.FirstOrDefaultAsync(x => x.Id == recharge.AppUserId);
                if (appUser is not null)
                {
                    appUser.Balance = appUser.Balance + recharge.Amount;
                }
            }
        }

        await db.SaveChangesAsync();

        return new TransactionRequesterDto(recharge.VendorId, recharge.MachineId, "");
    }
}
