using CMS.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers.Kiosk;

[Route("api/kiosk/[controller]")]
[ApiController]
public class ProductWithStockController : ControllerBase
{
    private readonly IUnitOfWork unitOfWork;

    public ProductWithStockController(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categoryItems = await unitOfWork.Categories.GetCategoryWithStockedProductsAsync();
        return Ok(categoryItems);
    }
}
