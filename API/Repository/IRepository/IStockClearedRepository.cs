using CMS.API.Domains;
using CMS.Dto;

namespace CMS.API.Repository.IRepository;

public interface IStockClearedRepository : IRepository<StockCleared>
{
    Task<List<RefillItemDto>> GetClearedRecords(string machineId, DateTime fromDate, DateTime toDate);
}
