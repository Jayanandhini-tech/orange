using CMS.API.Domains;
using CMS.Dto;

namespace CMS.API.Repository.IRepository;

public interface IStockMachineRepository : IRepository<StockMachine>
{
    Task<StockDisplayDto> GetMachineStocksAsync(string machineId);
    Task<List<StockReportDto>> GetMachineStocksByProductAsync(string machineId);
}
