using CMS.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<CompanyController> logger;

    public CompanyController(IUnitOfWork unitOfWork, ILogger<CompanyController> logger)
    {
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var company = await unitOfWork.Companies.GetAllAsync();
        return Ok(company);
    }
}

