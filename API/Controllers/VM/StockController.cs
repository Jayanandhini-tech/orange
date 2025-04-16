using CMS.API.Domains;
using CMS.API.Dtos;
using CMS.API.Extensions;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CMS.API.Controllers.VM;

[Route("api/vm/[controller]")]
[ApiController]
[Authorize(Roles = $"{SD.Role_Machine},{SD.Role_Admin}")]
public class StockController : ControllerBase
{
    private readonly ITenant tenant;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<StockController> logger;

    public StockController(ITenant tenant, IUnitOfWork unitOfWork, ILogger<StockController> logger)
    {
        this.tenant = tenant;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    [HttpGet("{machineId}")]
    public async Task<IActionResult> GetMachineStocks(string machineId)
    {
        var result = await unitOfWork.StockMachine.GetMachineStocksAsync(machineId);
        return Ok(result);
    }

    [HttpGet("product")]
    public async Task<IActionResult> GetStocksByProduct(string machineId)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var result = await unitOfWork.StockMachine.GetMachineStocksByProductAsync(machineId);
        return Ok(result);
    }

    [HttpGet("product/download")]
    public async Task<FileResult> DownloadStockbyProductReport(string machineId)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var result = await unitOfWork.StockMachine.GetMachineStocksByProductAsync(machineId);
        var file = ExcelHelper.CreateFile(result, $"Stock Report On {DateTime.Now.ToString("MMM dd yyyy hh:mm tt")}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockReport.xlsx");
    }


    [HttpGet("required")]
    public async Task<IActionResult> GetStockRequired(string machineId)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var stocks = await unitOfWork.StockMachine.GetMachineStocksByProductAsync(machineId);
        var required = stocks.Select(x => new StockRequirementDto(x.Product, x.Price, x.Capacity - x.Stock, (x.Capacity - x.Stock) * x.Price)).ToList();
        var result = required.Where(x => x.Required > 0).OrderBy(x => x.Product).ToList();
        return Ok(result ?? []);
    }

    [HttpGet("required/download")]
    public async Task<FileResult> DownloadStockRequiredReport(string machineId)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var stocks = await unitOfWork.StockMachine.GetMachineStocksByProductAsync(machineId);
        var required = stocks.Select(x => new StockRequirementDto(x.Product, x.Price, x.Capacity - x.Stock, (x.Capacity - x.Stock) * x.Price)).ToList();
        var result = required.Where(x => x.Required > 0).OrderBy(x => x.Product).ToList();
        var file = ExcelHelper.CreateFile(result, $"Stock Required Report On {DateTime.Now.ToString("MMM dd yyyy hh:mm tt")}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockReport.xlsx");
    }

    [HttpGet("refill")]
    public async Task<IActionResult> GetRefillRecords(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var results = await unitOfWork.StockRefills.GetRefillRecords(machineId, fromDate, toDate);
        return Ok(results ?? []);
    }

    [HttpGet("refill/download")]
    public async Task<IActionResult> GetRefillReport(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var results = await unitOfWork.StockRefills.GetRefillRecords(machineId, fromDate, toDate);


        var file = ExcelHelper.CreateFile(results, $"Stock Refill Report Between {fromDate.ToString("MMM dd yyyy hh:mm tt")} and {toDate.ToString("MMM dd yyyy hh:mm tt")}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockRefillReport.xlsx");
    }

    [HttpGet("cleared")]
    public async Task<IActionResult> GetClearedRecords(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var results = await unitOfWork.StockCleared.GetClearedRecords(machineId, fromDate, toDate);
        return Ok(results ?? []);
    }

    [HttpGet("cleared/download")]
    public async Task<IActionResult> GetClearedReport(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var results = await unitOfWork.StockCleared.GetClearedRecords(machineId, fromDate, toDate);

        var file = ExcelHelper.CreateFile(results, $"Stock Cleared Report Between {fromDate.ToString("MMM dd yyyy hh:mm tt")} and {toDate.ToString("MMM dd yyyy hh:mm tt")}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockClearedReport.xlsx");
    }


    [HttpGet("refillsummary")]
    public async Task<IActionResult> GetRefillSummaryRecords(string machineId, DateTime fromDate, DateTime toDate)
    {
        if (machineId.Trim().ToLower() == "all")
            machineId = string.Empty;

        var results = await unitOfWork.StockRefills.GetRefillSummary(machineId, fromDate, toDate);
        return Ok(results ?? []);
    }


    [HttpPost]
    public async Task<IActionResult> PostStockAynsc(List<StockMachineDto> stocksDto)
    {
        string machineId = tenant.MachineId;
        List<int> motorNumbers = [];      
        foreach (var stockM in stocksDto)
        {
            try
            {
                var stockDb = await unitOfWork.StockMachine.GetAsync(x => x.MachineId == machineId && x.CabinId == stockM.CabinId && x.MotorNumber == stockM.MotorNumber, tracked: true);
                if (stockDb is not null)
                {
                    stockDb.ProductId = string.IsNullOrEmpty(stockM.ProductId) ? null : stockM.ProductId;
                    stockDb.Capacity = stockM.Capacity;
                    stockDb.Stock = stockM.Stock;
                    stockDb.Soldout = stockM.SoldOut;
                    stockDb.UpdatedOn = stockM.UpdatedOn;
                    stockDb.IsActive = stockM.IsActive;
                }
                else
                {
                    await unitOfWork.StockMachine.AddAsync(new StockMachine()
                    {
                        Id = $"sm_{Guid.NewGuid().ToString("N").ToUpper()}",
                        MachineId = machineId,
                        CabinId = stockM.CabinId,
                        MotorNumber = stockM.MotorNumber,
                        ProductId = string.IsNullOrEmpty(stockM.ProductId) ? null : stockM.ProductId,
                        Capacity = stockM.Capacity,
                        Stock = stockM.Stock,
                        Soldout = stockM.SoldOut,
                        UpdatedOn = stockM.UpdatedOn,
                        IsActive = stockM.IsActive
                    });
                }
                await unitOfWork.SaveChangesAsync();
                motorNumbers.Add(stockM.MotorNumber);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }


        return Ok(new StockUpdateResultDto(motorNumbers));
    }

    [HttpPost("cleared")]
    public async Task<IActionResult> PostStockCleared(List<StockClearedDto> stockClearedDtos)
    {
        string machineId = tenant.MachineId;
        foreach (StockClearedDto clearedDto in stockClearedDtos)
        {

            var stockCleard = await unitOfWork.StockCleared.GetAsync(x => x.MachineId == machineId && x.Id == clearedDto.Id, tracked: true);

            if (stockCleard is not null)
                stockCleard.Quantity = clearedDto.Quantity;
            else
            {
                await unitOfWork.StockCleared.AddAsync(new StockCleared()
                {
                    Id = clearedDto.Id,
                    MachineId = machineId,
                    ClearedOn = clearedDto.ClearedOn,
                    MotorNumber = clearedDto.MotorNumber,
                    ProductId = clearedDto.ProductId,
                    ProductName = clearedDto.ProductName,
                    Quantity = clearedDto.Quantity
                });
            }
        }
        await unitOfWork.SaveChangesAsync();
        return Ok(stockClearedDtos.Select(x => x.Id).ToList());
    }

    [HttpPost("refill")]
    public async Task<IActionResult> PostStockRefill(List<StockRefillDto> stockRefillDtos)
    {
        string machineId = tenant.MachineId;

        foreach (StockRefillDto stockRefillDto in stockRefillDtos)
        {

            var stockRefill = await unitOfWork.StockRefills.GetAsync(x => x.MachineId == machineId && x.Id == stockRefillDto.Id, tracked: true);

            if (stockRefill is not null)
                stockRefill.Quantity = stockRefillDto.Quantity;
            else
            {
                await unitOfWork.StockRefills.AddAsync(new StockRefill()
                {
                    Id = stockRefillDto.Id,
                    MachineId = machineId,
                    RefilledOn = stockRefillDto.RefilledOn,
                    MotorNumber = stockRefillDto.MotorNumber,
                    ProductId = stockRefillDto.ProductId,
                    ProductName = stockRefillDto.ProductName,
                    Quantity = stockRefillDto.Quantity
                });
            }
        }
        await unitOfWork.SaveChangesAsync();
        return Ok(stockRefillDtos.Select(x => x.Id).ToList());
    }

}

