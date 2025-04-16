using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class PaymentSettingRepository : Repository<PaymentSetting>, IPaymentSettingRepository
{
    public PaymentSettingRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
