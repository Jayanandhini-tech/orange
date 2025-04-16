using CMS.API.Dtos;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers.VM;

[Route("api/vm/[controller]")]
[ApiController]
[Authorize(Roles = $"{SD.Role_Machine},{SD.Role_Admin}")]
public class MachinesController : ControllerBase
{
    private readonly ITenant tenant;
    private readonly IUnitOfWork db;

    public MachinesController(ITenant tenant, IUnitOfWork unitOfWork)
    {
        this.tenant = tenant;
        this.db = unitOfWork;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetInfoAsync()
    {
        var machine = await db.Machines.GetAsync(x => x.Id == tenant.MachineId);
        return Ok(machine.Adapt<MachineInfoDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        //  await hubContext.Clients.All.SendAsync("messgae", "Hello");
        await Task.CompletedTask;
        return Ok("Message sent");
    }
}
