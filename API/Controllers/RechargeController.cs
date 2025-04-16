using CMS.API.Payments.Dtos;
using CMS.API.Payments.Services;
using CMS.API.Payments.Services.Interface;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using CMS.API.Domains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Dto.Payments.QR;
using CMS.Dto.Payments;


namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RechargeController : ControllerBase
{
    private readonly IPaymentService paymentService;
    private readonly IDynamicQRCodeGenerator dynamicQRCode;
    private readonly IUnitOfWork unitOfWork;
    private readonly ITenant tenant;

    public RechargeController(IPaymentService paymentService, IDynamicQRCodeGenerator dynamicQRCode, IUnitOfWork unitOfWork, ITenant tenant)
    {
        this.paymentService = paymentService;
        this.dynamicQRCode = dynamicQRCode;
        this.unitOfWork = unitOfWork;
        this.tenant = tenant;
    }


    [HttpPost("upi/create")]
    public async Task<IActionResult> CreateQR(RechargeUpiDto rechargeDto)
    {
        var appUser = await unitOfWork.AppUsers.GetAsync(x => x.Id == rechargeDto.UserId);
        if (appUser is null)
            return Ok(new QrImageDto("", IsSuccess: false, Message: "User not found"));

        var vendor = await unitOfWork.Vendors.GetAsync(x => x.Id == tenant.VendorId);
        var machine = await unitOfWork.Machines.GetAsync(x => x.Id == tenant.MachineId);

        int count = await unitOfWork.Recharges.CountAsync(x => x.MachineId == tenant.MachineId);
        string Id = $"RC{vendor?.ShortName}{machine?.MachineNumber.ToString("00")}{tenant.AppType[0]}{count.ToString("0000")}";

        Recharge recharge = new Recharge()
        {
            Id = Id,
            MachineId = tenant.MachineId,
            AppUserId = appUser.Id,
            Amount = rechargeDto.Amount,
            IsSuccess = false,
            PaymentType = StrDir.PaymentType.UPI,
            PaymentProvider = paymentService.Gateway,
            Status = StrDir.OrderStatus.INITIATED,
            TransactionId = string.Empty,
            RechargedOn = DateTime.Now
        };
        await unitOfWork.Recharges.AddAsync(recharge);
        await unitOfWork.SaveChangesAsync();




        UPICreateResponse response = await paymentService.CreateQRAsync(recharge.Id, recharge.Amount, tenant.MachineId);

        if (!response.Success)
        {
            recharge.Status = StrDir.OrderStatus.FAILED;
            await unitOfWork.SaveChangesAsync();
            return Ok(new QrImageDto("", IsSuccess: false, Message: "Unable to generate QR code"));
        }

        string qrImage = dynamicQRCode.CreateQRfromUPIstring(response.UpiString);

        return Ok(new QrImageDto(recharge.Id, true, qrImage, response.DisplayName));

    }

    [HttpGet("upi/status")]
    public async Task<IActionResult> Status(string rechargeId)
    {

        var recharge = await unitOfWork.Recharges.GetAsync(x => x.Id == rechargeId);

        if (recharge is null)
            return Ok(new QrStatusDto(rechargeId, PaymentStatusCode.FAILED, "RechargeId not found"));

        return Ok(new QrStatusDto(rechargeId, Enum.Parse<PaymentStatusCode>(recharge.Status)));

    }

    [HttpGet("upi/status/gateway")]
    public async Task<IActionResult> StatusonGateway(string rechargeId)
    {

        UPIStatusResponse response = await paymentService.StatusAsync(rechargeId);

        if (!response.Success)
            return Ok(new QrStatusDto(rechargeId, PaymentStatusCode.FAILED, response.Message));


        await unitOfWork.Recharges.UpdateRechargeStatus(rechargeId, response.Status.ToString(), response.TransactionId);

        return Ok(new QrStatusDto(rechargeId, response.Status, response.Message));
    }

}
