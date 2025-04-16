using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class RefundRepository : Repository<Refund>, IRefundRepository
{
    public RefundRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
