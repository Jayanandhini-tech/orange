using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Services;

public class StockService : IStockService
{
    private readonly IUnitOfWork unitOfWork;

    public StockService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<bool> ReduceStockForKioskOrder(List<OrderItem> items)
    {
        foreach (var item in items)
        {
            var product = await unitOfWork.Products.GetAsync(x => x.Id == item.ProductId, tracked: true);
            if (product is not null)
            {
                product.Stock = product.Stock - item.Qty;
            }
        }

        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
