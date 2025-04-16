using CMS.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AppTypesController : ControllerBase
{
    private readonly IUnitOfWork unitOfWork;

    public AppTypesController(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var appTypes = await unitOfWork.Machines.GetUserAppTypesAsync();
        return Ok(appTypes);
    }
}
