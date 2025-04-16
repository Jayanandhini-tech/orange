using CMS.API.Dtos;
using CMS.API.Repository.IRepository;
using CMS.Dto;
using CMS.Dto.Stocks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers.Kiosk;

[Route("api/kiosk/[controller]")]
[ApiController]
[Authorize]
[Authorize(Roles = $"{SD.Role_Machine},{SD.Role_Admin}")]
public class StocksController : ControllerBase
{
    private readonly IUnitOfWork db;

    public StocksController(IUnitOfWork unitOfWork)
    {
        this.db = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetStocks()
    {
        var categoryItems = await db.Categories.GetCategoryWiseStocksAsync();
        return Ok(categoryItems);
    }


    [HttpPost]
    public async Task<IActionResult> UpdateStock(StockUpdateDto stockUpdateDto)
    {
        var product = await db.Products.GetById(stockUpdateDto.ProductId);
        if (product is null)
            return BadRequest("Product not found");

        product.Stock = stockUpdateDto.Stock;
        product.IsStocked = stockUpdateDto.IsAvailable;

        await db.SaveChangesAsync();

        return Ok(new SuccessDto("Stock updated successfully"));
    }
}

