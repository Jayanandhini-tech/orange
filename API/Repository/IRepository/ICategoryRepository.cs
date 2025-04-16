using CMS.API.Domains;
using CMS.Dto;
using CMS.Dto.Stocks;

namespace CMS.API.Repository.IRepository;

public interface ICategoryRepository : IRepository<Category>
{
    Task<List<CategoryWiseStock>> GetCategoryWiseStocksAsync();
    Task<List<CategoryIncludeProductWithStockDto>> GetCategoryWithStockedProductsAsync();
}
