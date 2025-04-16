using CMS.API.Domains;
using CMS.Dto;

namespace CMS.API.Repository.IRepository;

public interface ICashRefundRepository : IRepository<CashRefund>
{
    Task<List<CashRefundsDto>> GetCashRefund(DateTime fromDate, DateTime toDate);
    Task MarkAsPaid(string orderNumber, string mobileNumber);
}
