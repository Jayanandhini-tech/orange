using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using CMS.Dto.Stocks;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    private readonly AppDBContext db;
    private readonly ITenant tenant;

    public CategoryRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    public async Task<List<CategoryIncludeProductWithStockDto>> GetCategoryWithStockedProductsAsync()
    {
        var resutl = await db.Categories
                                .Include(c => c.Products)
                                .Where(x => x.VendorId == tenant.VendorId && x.AppType == tenant.AppType)
                                .Select(x =>
                                            new CategoryIncludeProductWithStockDto()
                                            {
                                                CategoryId = x.Id,
                                                CategoryName = x.Name,
                                                DisplayOrder = x.DisplayOrder,
                                                ImgPath = x.ImgPath,
                                                Products = x.Products!
                                                .Where(p => p.IsStocked == true && p.Stock > 0)
                                                .Select(p => new ProductDisplayDto()
                                                {
                                                    Id = p.Id,
                                                    Name = p.Name,
                                                    Rate = p.BaseRate,
                                                    ImgPath = p.ImgPath,
                                                    Gst = p.Gst,
                                                    Price = p.Price,
                                                    Stock = p.Stock
                                                }).ToList()
                                            }
                                          )
                                .OrderBy(x => x.DisplayOrder)
                                .ToListAsync();

        return resutl;
    }


    public async Task<List<CategoryWiseStock>> GetCategoryWiseStocksAsync()
    {
        var resutl = await db.Categories
                                .Include(c => c.Products)
                                .Where(x => x.VendorId == tenant.VendorId && x.AppType == StrDir.AppType.KIOSK)
                                .Select(x =>
                                            new CategoryWiseStock()
                                            {
                                                CategoryId = x.Id,
                                                CategoryName = x.Name,

                                                DisplayOrder = x.DisplayOrder,
                                                ImgPath = x.ImgPath,
                                                Products = x.Products!
                                                .Select(p => new StockProduct()
                                                {
                                                    Id = p.Id,
                                                    Name = p.Name,
                                                    Price = p.Price,
                                                    ImgPath = p.ImgPath,
                                                    IsAvailable = p.IsStocked,
                                                    Stock = p.Stock
                                                }).ToList()
                                            }
                                          )
                                .OrderBy(x => x.DisplayOrder)
                                .ToListAsync();

        return resutl;
    }

}
