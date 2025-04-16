using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class ProductRepository : Repository<Product>, IProductRepository
{
    private readonly AppDBContext db;


    public ProductRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
    }

    public async Task<Product?> GetById(string id)
    {
        return await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    }

}
