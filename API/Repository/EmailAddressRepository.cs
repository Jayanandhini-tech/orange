using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class EmailAddressRepository : Repository<EmailAddress>, IEmailAddressRepository
{
    public EmailAddressRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
