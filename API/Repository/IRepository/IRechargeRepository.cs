using CMS.API.Domains;
using CMS.API.Payments.Services;

namespace CMS.API.Repository.IRepository;

public interface IRechargeRepository : IRepository<Recharge>
{
    Task<List<Recharge>> GetUserRecentRechargeAsync(string userId);
    Task<TransactionRequesterDto?> UpdateRechargeStatus(string rechargeId, string status, string transactionId);
}
