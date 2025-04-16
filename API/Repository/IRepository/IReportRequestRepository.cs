using CMS.API.Domains;

namespace CMS.API.Repository.IRepository;

public interface IReportRequestRepository : IRepository<ReportRequest>
{
    Task<List<ReportRequest>> GetAllMachineRequestsAsync(string MachineId);
    Task<ReportRequest?> GetRequestByIdAsync(string Id);
}
