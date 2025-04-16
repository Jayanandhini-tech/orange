using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MachinesController : ControllerBase
{
    private readonly ITenant tenant;
    private readonly INotificationService notificationService;
    private readonly IUnitOfWork unitOfWork;
    private readonly AppDBContext dBContext;
    private readonly ILogger<MachinesController> logger;

    public MachinesController(ITenant tenant, INotificationService notificationService, IUnitOfWork unitOfWork, AppDBContext dBContext, ILogger<MachinesController> logger)
    {
        this.tenant = tenant;
        this.notificationService = notificationService;
        this.unitOfWork = unitOfWork;
        this.dBContext = dBContext;
        this.logger = logger;
    }



    [HttpGet("myinfo")]
    public async Task<IActionResult> GetInfoAsync()
    {
        var machine = await dBContext.Machines.FindAsync(tenant.MachineId);
        if (machine is null)
            return BadRequest("Machine not found");

        var vendor = await dBContext.Vendors.FindAsync(tenant.VendorId);

        var result = machine.Adapt<MachineInfoDto>();
        result.VendorShortName = vendor!.ShortName ?? vendor.Name!.Substring(0, 3);
        result.VendorShortName = result.VendorShortName.Trim().ToUpper();

        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetMachinesStatus()
    {
        var machines = await dBContext.Machines
                                      .Where(x => x.VendorId == tenant.VendorId && x.IsActive == true)
                                      .OrderBy(m => m.AppType)
                                      .ThenBy(m => m.MachineNumber)
                                      .Select(x => new MachineStatusDto(x.Id, x.AppType, x.MachineNumber, x.Name, x.Location, x.Status, x.UpdatedOn))
                                      .ToListAsync();

        return Ok(machines);
    }


    [HttpGet("paymentsettings")]
    public async Task<IActionResult> GetMachinePaymentSettings()
    {
        try
        {
            PaymentSettingsDto paymentSettingsDto = new() { Upi = true };

            string machineId = tenant.MachineId;
            var paymentOptions = await unitOfWork.PaymentSettings.GetAsync(x => x.MachineId == machineId, tracked: false);

            if (paymentOptions is not null)
            {
                paymentSettingsDto = paymentOptions.Adapt<PaymentSettingsDto>();

                if (paymentSettingsDto.Account)
                {
                    var accSets = await unitOfWork.PaymentAccountSettings.GetAsync(x => x.MachineId == machineId, tracked: false);
                    if (accSets is not null)
                    {
                        paymentSettingsDto.AccountSettings = accSets.Adapt<PaymentAccountSettingsDto>();
                        if (paymentSettingsDto.AccountSettings.AuthType == StrDir.AuthPage.FACE)
                        {
                            var faceSet = await unitOfWork.FaceDeviceSettings.GetAsync(x => x.MachineId == machineId, tracked: false);
                            if (faceSet is not null)
                            {
                                paymentSettingsDto.FaceDeviceSettings = faceSet.Adapt<FaceDeviceSettingsDto>();
                            }
                        }
                    }
                }
            }

            return Ok(paymentSettingsDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("sendmessageall")]
    public async Task<IActionResult> SendTestMessage(SuccessDto dto)
    {

        await notificationService.TestMessageToAllMachine(tenant.VendorId, dto.Message);
        return Ok("Message sent");
    }
}
