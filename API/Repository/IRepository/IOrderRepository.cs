using CMS.API.Domains;
using CMS.Dto;
using CMS.Dto.Reports;

namespace CMS.API.Repository.IRepository;

public interface IOrderRepository : IRepository<Order>
{
    Task<int> GetNextBillNumber();
    Task<List<OrderDownloadDto>> GetOrderForDownloadAsync(DateTime fromDate, DateTime toDate);
    Task<OrderReportResponseDto> GetOrderReportAsync(DateTime fromDate, DateTime toDate, int limit, int skip);
    Task<Order?> GetOrderUsingOrderNumber(string orderNumber, bool tracked = false);
    Task<Order?> GetOrderWithAllIncludes(string orderId);
    Task<List<DailySales>> GetOverAllLastWeekDailySales(string machineId = "");
    Task<List<MonthWiseSales>> GetOverAllMonthlySales(string machineId = "");
    Task<List<OrderItemReportDto>> GetProductBasedSales(string machineId, DateTime fromDate, DateTime toDate);
    Task<List<ProdcutSalesSumDto>> GetProductSalesForChart(string machineId);
    Task<SalesLegendsDto> GetSalesLegends(string machineId = "");
    Task<List<OrderReportDto>> GetSalesReportAsync(string machineId, DateTime fromDate, DateTime toDate);
    Task<List<SalesUsersDto>> GetUserBasedSales(string machineId, DateTime fromDate, DateTime toDate);
    Task<List<Order>> GetUserRecentOrders(string userId);
}
