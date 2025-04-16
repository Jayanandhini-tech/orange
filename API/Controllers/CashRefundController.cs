using CMS.API.Domains;
using CMS.API.Extensions;
using CMS.API.Payments.Dtos.ICICI;
using CMS.API.Repository.IRepository;
using CMS.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CashRefundController : ControllerBase
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<CashRefundController> logger;

    public CashRefundController(IUnitOfWork unitOfWork, ILogger<CashRefundController> logger)
    {
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListCashRefundAsync(DateTime fromDate, DateTime toDate)
    {
        var records = await unitOfWork.CashRefunds.GetCashRefund(fromDate, toDate);
        return Ok(records);
    }

    [HttpGet("download")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DownloadCashRefundAsync(DateTime fromDate, DateTime toDate)
    {
        var records = await unitOfWork.CashRefunds.GetCashRefund(fromDate, toDate);
        var file = ExcelHelper.CreateFile(records, $"Cash Refund Report Between {fromDate.ToString("MMM dd yyyy hh:mm tt")} and {toDate.ToString("MMM dd yyyy hh:mm tt")}");
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CashRefund.xlsx");
    }


    [HttpPost]
    public async Task<IActionResult> AddCashRefundAsync(CashRefundDto refundDto)
    {
        CashRefund cashRefund = new CashRefund()
        {
            RefId = refundDto.RefId,
            OrderNumber = refundDto.OrderNumber,
            MobileNumber = refundDto.MobileNumber,
            Amount = refundDto.Amount,
            Denomination = refundDto.Denomination,
            IsPaid = false,
            CancelOn = refundDto.CancelOn
        };

        await unitOfWork.CashRefunds.AddAsync(cashRefund);
        await unitOfWork.SaveChangesAsync();

        return Ok(new SuccessDto("Cash Refund Added Successfully"));
    }

    [HttpPost("sync")]
    public async Task<IActionResult> CashRefundUploadAsync(List<CashRefundDto> refundsDto)
    {
        List<string> successIds = [];
        try
        {
            foreach (var refund in refundsDto)
            {
                bool isExist = await unitOfWork.CashRefunds.AnyAsync(x => x.RefId == refund.RefId && x.MobileNumber == refund.MobileNumber);
                if (!isExist)
                {
                    CashRefund cashRefund = new CashRefund()
                    {
                        RefId = refund.RefId,
                        OrderNumber = refund.OrderNumber,
                        MobileNumber = refund.MobileNumber,
                        Amount = refund.Amount,
                        Denomination = refund.Denomination,
                        IsPaid = false,
                        CancelOn = refund.CancelOn
                    };

                    await unitOfWork.CashRefunds.AddAsync(cashRefund);
                    await unitOfWork.SaveChangesAsync();
                    successIds.Add(refund.RefId);
                }
                else
                {
                    successIds.Add(refund.RefId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }

        return Ok(successIds);
    }



    [HttpPost("paid")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeStatus(CashRefundDto refundDto)
    {
        await unitOfWork.CashRefunds.MarkAsPaid(refundDto.OrderNumber, refundDto.MobileNumber);
        return Ok(new SuccessDto("Updated Success"));
    }

}
