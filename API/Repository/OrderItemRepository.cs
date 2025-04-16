using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class OrderItemRepository : Repository<OrderItem>, IOrderItemRepository
{
    public OrderItemRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
