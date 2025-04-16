using CMS.Dto;
using VM.Domains;
using VM.Dtos;

namespace VM.Services.Interfaces;

public interface IReportService
{
    Task<ReportStatusDto> EmailReport(DateTime from, DateTime to, string path, string reportType);
    Task<bool> GenerateExcelReport(DateTime from, DateTime to, string path);
    Task<List<CurrentStockByProductDto>> GetCurrentStock();
    Task<List<RefillReportDto>> GetRefillItems(DateTime from, DateTime to);
    Task<List<Order>> GetSales(DateTime from, DateTime to);
    Task<List<SalesByUserDto>> GetSalesByUser(DateTime from, DateTime to);
    Task<List<SalesByProductDto>> GetSalesByProduct(DateTime from, DateTime to);
    Task<List<StockClearedReportDto>> GetStockCleared(DateTime from, DateTime to);
    Task<bool> SendTodayAutomatedReport();
    Task SendReportToServer(string requestId, DateTime from, DateTime to);
}