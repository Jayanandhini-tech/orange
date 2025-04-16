using CMS.API.Dtos;
using CMS.API.Repository.IRepository;
using CMS.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = SD.Role_Admin)]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork unitOfWork;


    public DashboardController(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    [HttpGet("saleslegends")]
    public async Task<IActionResult> GetSalesLegends()
    {
        var result = await unitOfWork.Orders.GetSalesLegends();
        return Ok(result);
    }


    [HttpGet("saleslegends/{machineId}")]
    public async Task<IActionResult> GetMachineSalesLegends(string machineId)
    {
        SalesLegendsDto result = new(new(0, 0, 0, 0, 0), new(0, 0, 0, 0, 0));

        bool machineExist = await unitOfWork.Machines.MachineBelongsToVendor(machineId);
        if (machineExist)
            result = await unitOfWork.Orders.GetSalesLegends(machineId);

        return Ok(result);
    }

    [HttpGet("monthlysales")]
    public async Task<IActionResult> GetOverallMonthWiseSales()
    {
        List<MonthWiseSales> result = await unitOfWork.Orders.GetOverAllMonthlySales();
        return Ok(result);
    }

    [HttpGet("monthlysales/{machineId}")]
    public async Task<IActionResult> GetOverallMonthWiseSales(string machineId)
    {
        List<MonthWiseSales> result = [];
        bool machineExist = await unitOfWork.Machines.MachineBelongsToVendor(machineId);

        if (machineExist)
            result = await unitOfWork.Orders.GetOverAllMonthlySales(machineId);

        return Ok(result);
    }


    [HttpGet("dailysales")]
    public async Task<IActionResult> GetOverallDailySales()
    {
        List<DailySales> result = await unitOfWork.Orders.GetOverAllLastWeekDailySales();
        return Ok(result);
    }


    [HttpGet("dailysales/{machineId}")]
    public async Task<IActionResult> GetOverallDailySales(string machineId)
    {
        List<DailySales> result = [];

        bool machineExist = await unitOfWork.Machines.MachineBelongsToVendor(machineId);

        if (machineExist)
            result = await unitOfWork.Orders.GetOverAllLastWeekDailySales(machineId);

        return Ok(result);
    }

    [HttpGet("productsales/{machineId}")]
    public async Task<IActionResult> GetProductSalesForChart(string machineId)
    {
        List<ProdcutSalesSumDto> result = [];

        bool machineExist = await unitOfWork.Machines.MachineBelongsToVendor(machineId);

        if (machineExist)
            result = await unitOfWork.Orders.GetProductSalesForChart(machineId);

        return Ok(result);
    }
}

