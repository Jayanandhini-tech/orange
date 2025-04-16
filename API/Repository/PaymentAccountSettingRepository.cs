using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class PaymentAccountSettingRepository : Repository<PaymentAccountSetting>, IPaymentAccountSettingRepository
{
    public PaymentAccountSettingRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
