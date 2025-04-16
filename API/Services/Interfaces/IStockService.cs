using CMS.API.Domains;

namespace CMS.API.Services.Interfaces;

public interface IStockService
{
    Task<bool> ReduceStockForKioskOrder(List<OrderItem> items);
}
