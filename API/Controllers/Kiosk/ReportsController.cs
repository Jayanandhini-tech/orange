using CMS.API.Extensions;
using CMS.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers.Kiosk;

[Route("api/kiosk/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork db;

    public ReportsController(IUnitOfWork unitOfWork)
    {
        this.db = unitOfWork;
    }


    [HttpGet("orders")]
    public async Task<IActionResult> GetReport(DateTime fromDate, DateTime toDate, int limit, int skip)
    {
        var response = await db.Orders.GetOrderReportAsync(fromDate, toDate, limit, skip);
        return Ok(response);
    }


    [HttpGet("download/orders")]    
    public async Task<FileResult> DownloadOrderReport(DateTime fromDate, DateTime toDate)
    {
        var orders = await db.Orders.GetOrderForDownloadAsync(fromDate, toDate);

        var file = ExcelHelper.CreateFile(orders);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "orders.xlsx");
    }


}
