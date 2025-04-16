using CMS.API.Domains;
using CMS.Dto;

namespace CMS.API.Repository.IRepository;

public interface IStockRefillRepository : IRepository<StockRefill>
{
    Task<List<RefillItemDto>> GetRefillRecords(string machineId, DateTime fromDate, DateTime toDate);
    Task<List<RefillSummaryDto>> GetRefillSummary(string machineId, DateTime fromDate, DateTime toDate);
}
