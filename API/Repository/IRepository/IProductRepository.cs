using CMS.API.Domains;

namespace CMS.API.Repository.IRepository;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetById(string id);
}
