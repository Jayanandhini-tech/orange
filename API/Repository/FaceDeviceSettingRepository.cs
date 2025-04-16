using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;

namespace CMS.API.Repository;

public class FaceDeviceSettingRepository : Repository<FaceDeviceSetting>, IFaceDeviceSettingRepository
{
    public FaceDeviceSettingRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
    }
}
