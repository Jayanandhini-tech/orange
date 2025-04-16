using CMS.API.Domains;

namespace CMS.API.Repository.IRepository;

public interface IMachineRepository : IRepository<Machine>
{
    Task<Machine?> GetMachineAsync(string machineId);
    Task<List<string>> GetUserAppTypesAsync();
    Task<bool> MachineBelongsToVendor(string machineId);
    public void Update(Machine  machine);
}
